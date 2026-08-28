// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core;
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    /// <summary>
    /// What fraction of a ventilation unit's airflow its controller calls for at a given control
    /// temperature - the Nuaire arrangement's "30% at 22 &#176;C rising to 100% at 26 &#176;C and above",
    /// expressed as data.
    /// <para>
    /// <b>This is template data, not an algorithm.</b> It is carried on
    /// <see cref="VentilationUnitTemplate"/>, beside the performance table, and no generic air handling
    /// unit code anywhere reads a hard-coded 22 or 26. A different product ramps between different
    /// temperatures, or in steps, or not at all; a different project fits a different controller to the
    /// same product. Every one of those is a different curve on a template rather than a branch in an
    /// algorithm, which is the only arrangement in which adding a second manufacturer is adding a file.
    /// </para>
    /// <para>
    /// <b>It is a curve, not a schedule and not an operating state.</b> It says what fraction goes with
    /// what temperature. <i>Which</i> temperature - a particular room's, an average, a sensor's - and
    /// what the resulting airflow then is, are Iteration 3 control questions this deliberately does not
    /// answer. Where one unit serves several rooms whose temperatures differ, something has to say which
    /// signal drives the controller, and this type is careful not to imply an answer.
    /// </para>
    /// <para>
    /// <b>Its domain policy is stored, not assumed.</b> The source states "100% at 26 degrees
    /// <i>and above</i>", which is a saturating statement, so the curve carries
    /// <see cref="PerformanceDomainPolicy.ClampToDomain"/> and the flat behaviour above 26 &#176;C is the
    /// controller's own, written down. A curve that genuinely should refuse outside its range says so on
    /// itself. Nothing infers it.
    /// </para>
    /// </summary>
    public class FlowFractionControlCurve : IJSAMObject
    {
        private VentilationUnitPerformanceTable ventilationUnitPerformanceTable;
        private PerformanceDomainPolicy performanceDomainPolicy = PerformanceDomainPolicy.ClampToDomain;

        public FlowFractionControlCurve()
        {
        }

        /// <summary>
        /// Builds a curve from paired control temperatures and flow fractions.
        /// </summary>
        /// <param name="controlTemperatures_C">The control temperatures [&#176;C], strictly increasing.</param>
        /// <param name="flowFractions">The matching fractions of full airflow, each between 0 and 1.</param>
        /// <param name="performanceDomainPolicy">
        /// What the curve does outside the stated temperatures. Defaults to
        /// <see cref="PerformanceDomainPolicy.ClampToDomain"/> because a control ramp normally saturates at
        /// both ends - but it is stored on the curve, so a curve that means something else can say so.
        /// </param>
        public FlowFractionControlCurve(IEnumerable<double> controlTemperatures_C, IEnumerable<double> flowFractions, PerformanceDomainPolicy performanceDomainPolicy = PerformanceDomainPolicy.ClampToDomain)
        {
            ventilationUnitPerformanceTable = new VentilationUnitPerformanceTable(
                [new VentilationUnitPerformanceAxis(VentilationUnitPerformanceAxis.Name_ControlTemperature, "degC", controlTemperatures_C)],
                [new VentilationUnitPerformanceOutput(VentilationUnitPerformanceOutput.Name_FlowFraction, "-", flowFractions)]);

            this.performanceDomainPolicy = performanceDomainPolicy;
        }

        public FlowFractionControlCurve(FlowFractionControlCurve flowFractionControlCurve)
        {
            if (flowFractionControlCurve is not null)
            {
                ventilationUnitPerformanceTable = flowFractionControlCurve.ventilationUnitPerformanceTable is null ? null : new VentilationUnitPerformanceTable(flowFractionControlCurve.ventilationUnitPerformanceTable);
                performanceDomainPolicy = flowFractionControlCurve.performanceDomainPolicy;
            }
        }

        public FlowFractionControlCurve(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        /// <summary>What this curve does outside the temperatures it states.</summary>
        public PerformanceDomainPolicy PerformanceDomainPolicy
        {
            get
            {
                return performanceDomainPolicy;
            }
        }

        /// <summary>The control temperatures [&#176;C] the curve is stated at, in order.</summary>
        public double[] ControlTemperatures_C
        {
            get
            {
                return ventilationUnitPerformanceTable?.Axis(VentilationUnitPerformanceAxis.Name_ControlTemperature)?.Values;
            }
        }

        /// <summary>The matching flow fractions, in order.</summary>
        public double[] FlowFractions
        {
            get
            {
                return ventilationUnitPerformanceTable?.Output(VentilationUnitPerformanceOutput.Name_FlowFraction)?.Values;
            }
        }

        /// <summary>The lowest control temperature [&#176;C] the curve states.</summary>
        public double MinimumControlTemperature_C
        {
            get
            {
                VentilationUnitPerformanceAxis ventilationUnitPerformanceAxis = ventilationUnitPerformanceTable?.Axis(VentilationUnitPerformanceAxis.Name_ControlTemperature);

                return ventilationUnitPerformanceAxis is null ? double.NaN : ventilationUnitPerformanceAxis.Minimum;
            }
        }

        /// <summary>The highest control temperature [&#176;C] the curve states.</summary>
        public double MaximumControlTemperature_C
        {
            get
            {
                VentilationUnitPerformanceAxis ventilationUnitPerformanceAxis = ventilationUnitPerformanceTable?.Axis(VentilationUnitPerformanceAxis.Name_ControlTemperature);

                return ventilationUnitPerformanceAxis is null ? double.NaN : ventilationUnitPerformanceAxis.Maximum;
            }
        }

        /// <summary>
        /// Whether the curve can be read: one control-temperature axis, one flow-fraction output, and
        /// every fraction between 0 and 1 inclusive.
        /// <para>
        /// A fraction outside 0 to 1 is refused rather than clamped. Negative is airflow running backwards
        /// and above one is a unit exceeding itself; either is a transcription mistake, and clamping it
        /// would produce a controller that looks reasonable and is not the one anybody wrote down.
        /// </para>
        /// </summary>
        public bool IsValid
        {
            get
            {
                if (ventilationUnitPerformanceTable is null || !ventilationUnitPerformanceTable.IsValid || ventilationUnitPerformanceTable.AxisCount != 1)
                {
                    return false;
                }

                if (ventilationUnitPerformanceTable.AxisIndex(VentilationUnitPerformanceAxis.Name_ControlTemperature) != 0)
                {
                    return false;
                }

                double[] flowFractions = FlowFractions;
                if (flowFractions is null || flowFractions.Length == 0)
                {
                    return false;
                }

                foreach (double flowFraction in flowFractions)
                {
                    if (flowFraction < 0 || flowFraction > 1)
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// The fraction of full airflow the controller calls for at a control temperature, under the
        /// curve's own domain policy.
        /// <para>
        /// Exactly the stated fraction at a stated temperature; linearly interpolated between two of them.
        /// </para>
        /// </summary>
        public double FlowFraction(double controlTemperature_C)
        {
            return FlowFraction(controlTemperature_C, performanceDomainPolicy);
        }

        /// <summary>
        /// The fraction of full airflow at a control temperature, under a policy the caller names -
        /// overriding the curve's own.
        /// <para>
        /// Provided so a study can ask what the curve would say if it refused beyond its stated range,
        /// without editing the catalogue. The override is on the call, never on the stored curve.
        /// </para>
        /// </summary>
        public double FlowFraction(double controlTemperature_C, PerformanceDomainPolicy performanceDomainPolicy)
        {
            if (!IsValid)
            {
                return double.NaN;
            }

            return ventilationUnitPerformanceTable.Value(VentilationUnitPerformanceOutput.Name_FlowFraction, [controlTemperature_C], performanceDomainPolicy);
        }

        /// <summary>Whether a control temperature falls inside the range the curve states.</summary>
        public bool InDomain(double controlTemperature_C)
        {
            return IsValid && ventilationUnitPerformanceTable.InDomain(controlTemperature_C);
        }

        public override string ToString()
        {
            if (!IsValid)
            {
                return "Invalid FlowFractionControlCurve";
            }

            double[] controlTemperatures_C = ControlTemperatures_C;
            double[] flowFractions = FlowFractions;

            List<string> points = [];
            for (int i = 0; i < controlTemperatures_C.Length; i++)
            {
                points.Add(string.Format("{0:0.###} degC -> {1:0.###}", controlTemperatures_C[i], flowFractions[i]));
            }

            return string.Format("{0} ({1} outside)", string.Join(", ", points), performanceDomainPolicy);
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject is null)
            {
                return false;
            }

            ventilationUnitPerformanceTable = jsonObject["PerformanceTable"] is JsonObject jsonObject_PerformanceTable ? new VentilationUnitPerformanceTable(jsonObject_PerformanceTable) : null;

            //An ABSENT policy reads as ClampToDomain - a control ramp normally saturates, and the shipped
            //catalogue states it anyway. A policy that is PRESENT but is not one of the names refuses the
            //whole curve, because the alternative is that a mistyped "Refuse" quietly becomes a clamp: the
            //author asked for the strict behaviour and would have been given the permissive one, which is
            //the one direction a typo must never take. Refused by emptying the curve, so IsValid is false
            //and every lookup answers NaN.
            performanceDomainPolicy = PerformanceDomainPolicy.ClampToDomain;

            string text = PerformanceJson.Text(jsonObject, "PerformanceDomainPolicy");

            if (!string.IsNullOrWhiteSpace(text))
            {
                PerformanceDomainPolicy performanceDomainPolicy_Temp = Core.Query.Enum<PerformanceDomainPolicy>(text);

                if (performanceDomainPolicy_Temp == PerformanceDomainPolicy.Undefined)
                {
                    ventilationUnitPerformanceTable = null;

                    return false;
                }

                performanceDomainPolicy = performanceDomainPolicy_Temp;
            }

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject result = new()
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            if (ventilationUnitPerformanceTable is not null)
            {
                result["PerformanceTable"] = ventilationUnitPerformanceTable.ToJsonObject();
            }

            result["PerformanceDomainPolicy"] = performanceDomainPolicy.ToString();

            return result;
        }
    }
}
