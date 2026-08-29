// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core;
using SAM.Math;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    /// <summary>
    /// A manufacturer's published performance table, exactly as published: the conditions it was measured
    /// at, and every measured value.
    /// <para>
    /// <b>Raw data, not a fitted model.</b> The whole reason this type exists is that the industry habit
    /// is to reduce a table like this to a regression, a spreadsheet slice or a single corner value early
    /// and then carry the reduction around as though it were the manufacturer's word. The legacy route
    /// this replaces did exactly that twice over - it kept one 80 l/s slice of a three-dimensional table,
    /// and it generated an equation to paste into a simulation tool. Both are derived representations,
    /// both are defensible, and neither can be checked against the brochure afterwards. Here the brochure
    /// is what is stored, and every derived thing is computed from it on demand under a named policy.
    /// </para>
    /// <para>
    /// <b>Shape is data.</b> Axes are a list, so a two-condition table and a four-condition table are the
    /// same type; outputs are a list, so a product that publishes an input power as well as a supply
    /// temperature needs no schema change. Nothing here knows what a heat recovery unit is - it is a
    /// labelled grid of numbers with a source, and the meaning lives in
    /// <see cref="VentilationUnitTemplate"/> and in whoever reads it.
    /// </para>
    /// <para>
    /// <b>Dormant in Approved Document O Iteration 2.</b> Nothing in the sizing kernel reads this. It is
    /// carried so that Iteration 3 - which has to produce an hourly leaving-air temperature and airflow
    /// for a Tas Systems model - inherits the manufacturer's own numbers rather than having to go back to
    /// the brochure. Selection uses <see cref="VentilationUnitCapacityDescriptor"/> and nothing else.
    /// </para>
    /// </summary>
    public class VentilationUnitPerformanceTable : IJSAMObject
    {
        private List<VentilationUnitPerformanceAxis> axes;
        private List<VentilationUnitPerformanceOutput> outputs;

        /// <summary>
        /// The interpolators, one per output, built once from the axes and cached. Rebuilt from scratch
        /// whenever the table is reloaded, so a cached interpolator can never outlive the data it came
        /// from.
        /// </summary>
        private Dictionary<string, Math.MultilinearInterpolation> multilinearInterpolations;

        /// <summary>
        /// Guards the cache above. Iteration 3 is expected to read one shared catalogue table across eight
        /// thousand timesteps, and a Dictionary written from two threads at once corrupts silently - which
        /// on a performance lookup means wrong numbers rather than an exception. The lock is taken only
        /// while the cache is consulted, never while a value is interpolated.
        /// </summary>
        private readonly object @lock = new();

        /// <summary>
        /// Incremented, under the lock above, every time <see cref="FromJsonObject"/> replaces the
        /// axes/outputs. An interpolator build tags itself with the generation it read its snapshot under,
        /// so a build started against generation N can never be published into generation N+1's cache - see
        /// <see cref="Interpolation(string)"/>.
        /// </summary>
        private int generation;

        /// <summary>
        /// Test-only seam. Invoked, outside any lock, immediately after <see cref="Interpolation(string)"/>
        /// captures its pre-build snapshot (axes, output and generation) and before it builds the
        /// interpolator - the exact window a concurrent <see cref="FromJsonObject"/> reload occupies in the
        /// race this field's sibling <see cref="generation"/> field closes. Production code never sets this.
        /// </summary>
        internal Action OnInterpolationSnapshotCaptured;

        public VentilationUnitPerformanceTable()
        {
        }

        public VentilationUnitPerformanceTable(IEnumerable<VentilationUnitPerformanceAxis> axes, IEnumerable<VentilationUnitPerformanceOutput> outputs)
        {
            //Copied: both are mutable-ish reference types, and a table a lookup is reading must not change
            //underneath it - the same rule VentilationUnitCapacityDescriptor follows for its reference.
            if (axes is not null)
            {
                this.axes = [];
                foreach (VentilationUnitPerformanceAxis ventilationUnitPerformanceAxis in axes)
                {
                    this.axes.Add(ventilationUnitPerformanceAxis is null ? null : new VentilationUnitPerformanceAxis(ventilationUnitPerformanceAxis));
                }
            }

            if (outputs is not null)
            {
                this.outputs = [];
                foreach (VentilationUnitPerformanceOutput ventilationUnitPerformanceOutput in outputs)
                {
                    this.outputs.Add(ventilationUnitPerformanceOutput is null ? null : new VentilationUnitPerformanceOutput(ventilationUnitPerformanceOutput));
                }
            }
        }

        public VentilationUnitPerformanceTable(VentilationUnitPerformanceTable ventilationUnitPerformanceTable)
            : this(ventilationUnitPerformanceTable?.axes, ventilationUnitPerformanceTable?.outputs)
        {
        }

        public VentilationUnitPerformanceTable(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        /// <summary>How many independent conditions the table is tabulated against. -1 where it holds nothing.</summary>
        public int AxisCount
        {
            get
            {
                return axes is null ? -1 : axes.Count;
            }
        }

        /// <summary>The names of the axes, in order.</summary>
        public List<string> AxisNames
        {
            get
            {
                if (axes is null)
                {
                    return null;
                }

                List<string> result = [];
                foreach (VentilationUnitPerformanceAxis ventilationUnitPerformanceAxis in axes)
                {
                    result.Add(ventilationUnitPerformanceAxis?.Name);
                }

                return result;
            }
        }

        /// <summary>The names of the outputs, in order.</summary>
        public List<string> OutputNames
        {
            get
            {
                if (outputs is null)
                {
                    return null;
                }

                List<string> result = [];
                foreach (VentilationUnitPerformanceOutput ventilationUnitPerformanceOutput in outputs)
                {
                    result.Add(ventilationUnitPerformanceOutput?.Name);
                }

                return result;
            }
        }

        /// <summary>How many conditions the table was measured at - the product of the axis lengths.</summary>
        public int PointCount
        {
            get
            {
                if (axes is null || axes.Count == 0)
                {
                    return -1;
                }

                long result = 1;

                foreach (VentilationUnitPerformanceAxis ventilationUnitPerformanceAxis in axes)
                {
                    if (ventilationUnitPerformanceAxis is null || ventilationUnitPerformanceAxis.Count <= 0)
                    {
                        return -1;
                    }

                    result *= ventilationUnitPerformanceAxis.Count;

                    if (result > int.MaxValue)
                    {
                        return -1;
                    }
                }

                return (int)result;
            }
        }

        /// <summary>
        /// Whether this is a table a lookup can be answered from.
        /// <para>
        /// Every axis and every output has to be valid in its own right, every axis name has to be
        /// distinct, every output name has to be distinct, and - the check that matters most - every
        /// output has to hold exactly one value per grid point. An output one value short of the grid is
        /// not a smaller table; it is a table whose every value from the mistake onwards is attributed to
        /// the wrong conditions, and there is no way to detect that later.
        /// </para>
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (axes is null || axes.Count == 0 || outputs is null || outputs.Count == 0)
                {
                    return false;
                }

                int pointCount = PointCount;
                if (pointCount <= 0)
                {
                    return false;
                }

                HashSet<string> names = [];

                foreach (VentilationUnitPerformanceAxis ventilationUnitPerformanceAxis in axes)
                {
                    if (ventilationUnitPerformanceAxis is null || !ventilationUnitPerformanceAxis.IsValid || !names.Add(ventilationUnitPerformanceAxis.Name))
                    {
                        return false;
                    }
                }

                names = [];

                foreach (VentilationUnitPerformanceOutput ventilationUnitPerformanceOutput in outputs)
                {
                    if (ventilationUnitPerformanceOutput is null || !ventilationUnitPerformanceOutput.IsValid || !names.Add(ventilationUnitPerformanceOutput.Name))
                    {
                        return false;
                    }

                    if (ventilationUnitPerformanceOutput.Count != pointCount)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>The position of a named axis, or -1 where the table has no such axis.</summary>
        public int AxisIndex(string name)
        {
            if (axes is null)
            {
                return -1;
            }

            for (int i = 0; i < axes.Count; i++)
            {
                if (axes[i] is not null && axes[i].Matches(name))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>One axis by position, or null. A copy.</summary>
        public VentilationUnitPerformanceAxis Axis(int index)
        {
            if (axes is null || index < 0 || index >= axes.Count || axes[index] is null)
            {
                return null;
            }

            return new VentilationUnitPerformanceAxis(axes[index]);
        }

        /// <summary>One axis by name, or null. A copy.</summary>
        public VentilationUnitPerformanceAxis Axis(string name)
        {
            return Axis(AxisIndex(name));
        }

        /// <summary>One output by name, or null. A copy.</summary>
        public VentilationUnitPerformanceOutput Output(string name)
        {
            if (outputs is null)
            {
                return null;
            }

            foreach (VentilationUnitPerformanceOutput ventilationUnitPerformanceOutput in outputs)
            {
                if (ventilationUnitPerformanceOutput is not null && ventilationUnitPerformanceOutput.Matches(name))
                {
                    return new VentilationUnitPerformanceOutput(ventilationUnitPerformanceOutput);
                }
            }

            return null;
        }

        /// <summary>
        /// One published value, addressed by its position on each axis - the number as transcribed from the
        /// document, with no arithmetic performed on it.
        /// <para>
        /// This is what a test pins a brochure figure with, and what a report quotes. It is deliberately a
        /// differently named method from <see cref="Value(string, double[], PerformanceDomainPolicy)"/>:
        /// "the manufacturer says 15.7" and "we worked out 15.7" are different claims, and the two must
        /// never be reachable by an accident of overload resolution.
        /// </para>
        /// </summary>
        public double PublishedValue(string outputName, params int[] indices)
        {
            if (!IsValid || indices is null || indices.Length != axes.Count)
            {
                return double.NaN;
            }

            VentilationUnitPerformanceOutput ventilationUnitPerformanceOutput = OutputInternal(outputName);
            if (ventilationUnitPerformanceOutput is null)
            {
                return double.NaN;
            }

            int index = 0;

            for (int i = 0; i < indices.Length; i++)
            {
                if (indices[i] < 0 || indices[i] >= axes[i].Count)
                {
                    return double.NaN;
                }

                index = (index * axes[i].Count) + indices[i];
            }

            double[] values = ventilationUnitPerformanceOutput.Values;

            return index >= 0 && index < values.Length ? values[index] : double.NaN;
        }

        /// <summary>
        /// The value of one output at one set of conditions.
        /// <para>
        /// At a published condition this returns the published number exactly - the interpolation weights
        /// are exactly zero and one there. Between published conditions it is multilinear over the
        /// surrounding cell, which is deterministic and independent of how the table was ordered or read.
        /// </para>
        /// <para>
        /// <b>Outside the published conditions the answer depends on <paramref name="performanceDomainPolicy"/>,
        /// and the default is to refuse.</b> See <see cref="PerformanceDomainPolicy"/> - the point is that
        /// a number the manufacturer never published can never be produced by accident.
        /// </para>
        /// </summary>
        /// <param name="outputName">Which published quantity - see <c>VentilationUnitPerformanceOutput.Name_</c>.</param>
        /// <param name="coordinates">One coordinate per axis, in axis order.</param>
        /// <param name="performanceDomainPolicy">What to do beyond the published conditions.</param>
        public double Value(string outputName, double[] coordinates, PerformanceDomainPolicy performanceDomainPolicy = PerformanceDomainPolicy.Refuse)
        {
            Math.MultilinearInterpolation multilinearInterpolation = Interpolation(outputName);

            if (multilinearInterpolation is null || coordinates is null)
            {
                return double.NaN;
            }

            switch (performanceDomainPolicy)
            {
                case PerformanceDomainPolicy.ClampToDomain:
                    return multilinearInterpolation.CalculateClamped(coordinates);

                case PerformanceDomainPolicy.OuterCellLinearExtrapolation:
                    return multilinearInterpolation.CalculateExtrapolated(coordinates);

                default:
                    //Undefined falls here with Refuse. An unrecognised policy is not a licence to extrapolate,
                    //and defaulting the other way would make a forgotten argument the permissive one.
                    return multilinearInterpolation.Calculate(coordinates);
            }
        }

        /// <summary>
        /// Whether a set of conditions falls inside everything the manufacturer published. Conditions
        /// exactly on a published boundary are inside it.
        /// </summary>
        public bool InDomain(params double[] coordinates)
        {
            if (!IsValid || coordinates is null || coordinates.Length != axes.Count)
            {
                return false;
            }

            for (int i = 0; i < coordinates.Length; i++)
            {
                if (double.IsNaN(coordinates[i]) || coordinates[i] < axes[i].Minimum || coordinates[i] > axes[i].Maximum)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The interpolator behind one output, built from the axes and cached. Null where the table is not
        /// valid or has no such output.
        /// <para>
        /// <b>Private.</b> Every read goes through <see cref="Value(string, double[], PerformanceDomainPolicy)"/>,
        /// which is the method that also applies the domain policy - so there is no way to reach the
        /// arithmetic while bypassing the decision about what happens outside the published range. It also
        /// keeps <c>SAM.Math</c> out of this type's public surface.
        /// </para>
        /// </summary>
        private Math.MultilinearInterpolation Interpolation(string outputName)
        {
            if (!IsValid || string.IsNullOrWhiteSpace(outputName))
            {
                return null;
            }

            Math.MultilinearInterpolation result;

            List<VentilationUnitPerformanceAxis> axesSnapshot;
            VentilationUnitPerformanceOutput outputSnapshot;
            int generationSnapshot;

            lock (@lock)
            {
                multilinearInterpolations ??= [];

                if (multilinearInterpolations.TryGetValue(outputName, out result))
                {
                    return result;
                }

                //Captured together, under the same lock a reload clears the cache and bumps the generation
                //under - so this snapshot is either entirely before or entirely after any one reload, never
                //a mix of pre- and post-reload state.
                generationSnapshot = generation;
                axesSnapshot = axes;
                outputSnapshot = OutputInternal(outputName);
            }

            if (outputSnapshot is null)
            {
                return null;
            }

            //Test-only: lets a test force a FromJsonObject reload here, deterministically, in the exact
            //window a concurrent one would otherwise race this build in.
            OnInterpolationSnapshotCaptured?.Invoke();

            List<double[]> axisValues = [];
            foreach (VentilationUnitPerformanceAxis ventilationUnitPerformanceAxis in axesSnapshot)
            {
                axisValues.Add(ventilationUnitPerformanceAxis.Values);
            }

            result = new Math.MultilinearInterpolation(axisValues, outputSnapshot.Values);

            if (!result.IsValid)
            {
                return null;
            }

            lock (@lock)
            {
                if (generation != generationSnapshot)
                {
                    //A reload replaced the table's data while this interpolator was being built from the
                    //pre-reload snapshot above. The result is correct for the table it was built from, but
                    //that table is no longer this generation - it must not be published into the current
                    //cache, or a later lookup for the same output would silently read the old table's value.
                    return result;
                }

                //Re-created rather than assumed: a concurrent reload will have cleared it, and an interpolator
                //this call already built is still worth returning to its caller.
                multilinearInterpolations ??= [];

                //Last writer wins, and that is harmless: two threads racing here on the SAME generation build
                //interpolators that are equal in every value, because both read the same immutable axes and
                //output.
                multilinearInterpolations[outputName] = result;
            }

            return result;
        }

        public override string ToString()
        {
            if (!IsValid)
            {
                return "Invalid VentilationUnitPerformanceTable";
            }

            return string.Format("{0} point(s) over {1}, publishing {2}", PointCount, string.Join(" x ", AxisNames), string.Join(", ", OutputNames));
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject is null)
            {
                return false;
            }

            axes = null;
            outputs = null;

            lock (@lock)
            {
                //Cleared with the data it was built from, so a cached interpolator can never outlive it.
                multilinearInterpolations = null;

                //Bumped in the same lock as the clear above, so a build already in flight against the old
                //data (see Interpolation) can tell it is no longer current even after this method has gone
                //on to install the new axes/outputs below.
                generation++;
            }

            if (jsonObject["Axes"] is JsonArray jsonArray_Axes)
            {
                axes = [];
                foreach (JsonNode jsonNode in jsonArray_Axes)
                {
                    axes.Add(jsonNode is JsonObject jsonObject_Axis ? new VentilationUnitPerformanceAxis(jsonObject_Axis) : null);
                }
            }

            if (jsonObject["Outputs"] is JsonArray jsonArray_Outputs)
            {
                outputs = [];
                foreach (JsonNode jsonNode in jsonArray_Outputs)
                {
                    outputs.Add(jsonNode is JsonObject jsonObject_Output ? new VentilationUnitPerformanceOutput(jsonObject_Output) : null);
                }
            }

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject result = new()
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (axes is not null)
            {
                JsonArray jsonArray = [];
                foreach (VentilationUnitPerformanceAxis ventilationUnitPerformanceAxis in axes)
                {
                    jsonArray.Add(ventilationUnitPerformanceAxis?.ToJsonObject());
                }

                result["Axes"] = jsonArray;
            }

            if (outputs is not null)
            {
                JsonArray jsonArray = [];
                foreach (VentilationUnitPerformanceOutput ventilationUnitPerformanceOutput in outputs)
                {
                    jsonArray.Add(ventilationUnitPerformanceOutput?.ToJsonObject());
                }

                result["Outputs"] = jsonArray;
            }

            return result;
        }

        /// <summary>The stored output, uncopied - for the internals that only read it.</summary>
        private VentilationUnitPerformanceOutput OutputInternal(string name)
        {
            if (outputs is null || string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            foreach (VentilationUnitPerformanceOutput ventilationUnitPerformanceOutput in outputs)
            {
                if (ventilationUnitPerformanceOutput is not null && ventilationUnitPerformanceOutput.Matches(name))
                {
                    return ventilationUnitPerformanceOutput;
                }
            }

            return null;
        }
    }
}
