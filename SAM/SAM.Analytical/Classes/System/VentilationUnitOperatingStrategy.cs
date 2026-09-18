// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using SAM.Core;
using System;
using System.Globalization;
using System.Text.Json.Nodes;

namespace SAM.Analytical
{
    /// <summary>
    /// The operating strategy a manufacturer states for its own unit in a dynamic thermal model: which mode
    /// the unit runs in at a given pair of air temperatures, what it then delivers, and at what airflow.
    /// <para>
    /// <b>Data, not an algorithm, and not a manufacturer's name.</b> Every threshold, fraction and limit
    /// below is stated on this object and comes from a catalogue entry with a traceable
    /// <see cref="Source"/>. No engineering code anywhere tests a manufacturer or model name, and no
    /// threshold from any one product's guidance is written as a constant in code. A second manufacturer
    /// with a different strategy is a second catalogue entry, not a branch.
    /// </para>
    /// <para>
    /// <b>This is manufacturer MODELLING GUIDANCE, and is not certified performance.</b> A strategy of this
    /// kind is what a manufacturer recommends for representing its unit's control behaviour. It carries no
    /// certified heat-recovery efficiency and no certified fan power, it satisfies neither, and it must
    /// never be presented as either - see <see cref="SupplyTemperatureRule"/>, and
    /// <see cref="VentilationUnitTemplate.HeatRecoveryPerformance"/> /
    /// <see cref="VentilationUnitTemplate.FanPerformance"/>, which exist for certified figures and are
    /// untouched by anything here.
    /// </para>
    /// <para>
    /// <b>The control temperatures are the unit's own sensors.</b> <c>intake</c> is the outdoor air arriving
    /// at the unit's intake and <c>extract</c> is the air returning to it from the dwelling - the two
    /// positions a unit of this kind physically senses. Neither is "a room temperature": where an
    /// authoritative system extract state exists, that is the signal, and a resolver that cannot obtain the
    /// temperatures at those two positions refuses rather than substituting a convenient one.
    /// </para>
    /// <para>
    /// <b>Exactly one mode, always.</b> Manufacturers usually publish these as several independently-written
    /// conditions, asserting that no two can be true together. <see cref="OperatingMode"/> evaluates them as
    /// one ordered decision, so every pair of temperatures selects exactly one mode - including pairs at
    /// which independently-written conditions are all false, which fall through to the stated default mode
    /// rather than leaving the unit undefined.
    /// </para>
    /// <para>
    /// <b>Four airflows stay four airflows.</b> Nothing here writes a design airflow.
    /// <see cref="ElevatedAirFlow_Lps"/> is what the unit moves while cooling - an operating airflow - and
    /// the background modes move whatever the dwelling was designed to move, which this object is handed and
    /// never alters:
    /// <c>PartFRequiredAirFlow != DesignAirFlow != SelectedEquipmentCapacity != OperatingAirFlow</c>.
    /// </para>
    /// </summary>
    public class VentilationUnitOperatingStrategy : IJSAMObject
    {
        public VentilationUnitOperatingStrategy()
        {
        }

        public VentilationUnitOperatingStrategy(VentilationUnitOperatingStrategy ventilationUnitOperatingStrategy)
        {
            if (ventilationUnitOperatingStrategy is not null)
            {
                Source = ventilationUnitOperatingStrategy.Source;
                CoolingActivationTemperature_C = ventilationUnitOperatingStrategy.CoolingActivationTemperature_C;
                MinimumCoolingActivationTemperature_C = ventilationUnitOperatingStrategy.MinimumCoolingActivationTemperature_C;
                MaximumCoolingActivationTemperature_C = ventilationUnitOperatingStrategy.MaximumCoolingActivationTemperature_C;
                BypassMinimumIntakeTemperature_C = ventilationUnitOperatingStrategy.BypassMinimumIntakeTemperature_C;
                BypassMinimumExtractTemperature_C = ventilationUnitOperatingStrategy.BypassMinimumExtractTemperature_C;
                ElevatedAirFlow_Lps = ventilationUnitOperatingStrategy.ElevatedAirFlow_Lps;
                MinimumElevatedAirFlow_Lps = ventilationUnitOperatingStrategy.MinimumElevatedAirFlow_Lps;
                MaximumElevatedAirFlow_Lps = ventilationUnitOperatingStrategy.MaximumElevatedAirFlow_Lps;
                SummerBypassSupplyTemperatureRule = Copy(ventilationUnitOperatingStrategy.SummerBypassSupplyTemperatureRule);
                HeatCoolthRecoverySupplyTemperatureRule = Copy(ventilationUnitOperatingStrategy.HeatCoolthRecoverySupplyTemperatureRule);
                CoolingSupplyTemperatureRule = Copy(ventilationUnitOperatingStrategy.CoolingSupplyTemperatureRule);
            }
        }

        public VentilationUnitOperatingStrategy(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        /// <summary>
        /// Where this strategy came from: the document, its issue, its date. <b>Required.</b> A control
        /// strategy nobody can trace back to a manufacturer's own words is not manufacturer guidance, and
        /// a compliance assessment that rests on one has nothing to cite.
        /// <para>
        /// Where the source is held privately - manufacturer guidance is frequently supplied under
        /// restriction and cannot be redistributed - this states what it is, who wrote it and when, not its
        /// contents.
        /// </para>
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// The extract-air temperature [&#176;C] above which the unit's cooling runs.
        /// <para>
        /// <b>Explicit data, and the one figure a project is expected to set.</b> Manufacturers state a
        /// range this may be moved within according to the overheating risk being assessed - see
        /// <see cref="MinimumCoolingActivationTemperature_C"/> and
        /// <see cref="MaximumCoolingActivationTemperature_C"/>, which <see cref="Refusal"/> holds it to. A
        /// project that wants a different setpoint copies the strategy with
        /// <see cref="WithCoolingActivationTemperature"/>; nothing anywhere hard-codes it.
        /// </para>
        /// </summary>
        public double CoolingActivationTemperature_C { get; set; } = double.NaN;

        /// <summary>The lowest cooling activation temperature [&#176;C] the manufacturer's guidance allows.</summary>
        public double MinimumCoolingActivationTemperature_C { get; set; } = double.NaN;

        /// <summary>The highest cooling activation temperature [&#176;C] the manufacturer's guidance allows.</summary>
        public double MaximumCoolingActivationTemperature_C { get; set; } = double.NaN;

        /// <summary>
        /// The intake-air temperature [&#176;C] the bypass requires to be exceeded. At or below it the unit
        /// recovers instead: bypassing cold intake air would deliver it into the dwelling unrecovered.
        /// </summary>
        public double BypassMinimumIntakeTemperature_C { get; set; } = double.NaN;

        /// <summary>
        /// The extract-air temperature [&#176;C] the bypass requires to be exceeded. At or below it the
        /// dwelling is not warm enough for bypassing to be the useful direction.
        /// </summary>
        public double BypassMinimumExtractTemperature_C { get; set; } = double.NaN;

        /// <summary>
        /// The total airflow [l/s] the unit moves while cooling - <b>an operating airflow</b>, never a design
        /// airflow and never the unit's capacity.
        /// <para>
        /// A stated figure rather than a derived one. The manufacturer states a range it is normally chosen
        /// within - see <see cref="MinimumElevatedAirFlow_Lps"/> and
        /// <see cref="MaximumElevatedAirFlow_Lps"/> - and how far up that range a particular dwelling goes
        /// depends on the overheating risk being mitigated, which is a design decision on the record rather
        /// than something to infer from a table's largest axis value.
        /// </para>
        /// </summary>
        public double ElevatedAirFlow_Lps { get; set; } = double.NaN;

        /// <summary>The lowest elevated airflow [l/s] the manufacturer's guidance states for cooling.</summary>
        public double MinimumElevatedAirFlow_Lps { get; set; } = double.NaN;

        /// <summary>The highest elevated airflow [l/s] the manufacturer's guidance states for cooling.</summary>
        public double MaximumElevatedAirFlow_Lps { get; set; } = double.NaN;

        /// <summary>What the unit delivers while bypassing.</summary>
        public SupplyTemperatureRule SummerBypassSupplyTemperatureRule { get; set; }

        /// <summary>What the unit delivers while recovering heat or coolth.</summary>
        public SupplyTemperatureRule HeatCoolthRecoverySupplyTemperatureRule { get; set; }

        /// <summary>What the unit delivers while cooling.</summary>
        public SupplyTemperatureRule CoolingSupplyTemperatureRule { get; set; }

        /// <summary>
        /// A copy of this strategy with a different cooling activation temperature - the supported way to
        /// change the setpoint, because the strategy a catalogue states is never mutated by a project that
        /// uses it.
        /// <para>
        /// The copy is validated exactly as any other strategy is: a setpoint outside the manufacturer's
        /// stated range refuses, it does not clamp.
        /// </para>
        /// </summary>
        public VentilationUnitOperatingStrategy WithCoolingActivationTemperature(double coolingActivationTemperature_C)
        {
            return new VentilationUnitOperatingStrategy(this) { CoolingActivationTemperature_C = coolingActivationTemperature_C };
        }

        /// <summary>
        /// A copy of this strategy with the elevated cooling airflow this dwelling is to be assessed at -
        /// the one figure a catalogue cannot state, because how far up the manufacturer's range a dwelling
        /// goes depends on the overheating risk being mitigated.
        /// </summary>
        public VentilationUnitOperatingStrategy WithElevatedAirFlow(double elevatedAirFlow_Lps)
        {
            return new VentilationUnitOperatingStrategy(this) { ElevatedAirFlow_Lps = elevatedAirFlow_Lps };
        }

        /// <summary>
        /// Whether the elevated cooling airflow has been resolved for a particular dwelling. A catalogue
        /// states the manufacturer's range; a project states the figure inside it - see
        /// <see cref="WithElevatedAirFlow"/>.
        /// </summary>
        public bool IsResolved
        {
            get
            {
                return IsFinite(ElevatedAirFlow_Lps);
            }
        }

        /// <summary>
        /// Which mode the manufacturer's control logic selects, for one hour's intake and extract air
        /// temperatures.
        /// <para>
        /// <b>Ordered, exclusive and strict.</b> Cooling first, on the extract temperature alone; then
        /// bypass, which requires every one of its stated conditions; then recovery, which is what is left.
        /// Every comparison is strict, so a temperature exactly at a threshold is <i>not</i> above it: at
        /// the activation temperature the unit is not cooling, at the bypass intake or extract limits it is
        /// not bypassing, and with extract equal to intake it is not bypassing. A manufacturer writing
        /// "&gt; 12" and "&lt; 12" as two separate conditions leaves 12 itself in neither; the ordering here
        /// gives that hour to the default mode rather than to no mode at all.
        /// </para>
        /// </summary>
        /// <param name="intakeTemperature_C">The outdoor / intake air temperature [&#176;C] at the unit.</param>
        /// <param name="extractTemperature_C">The extract / return air temperature [&#176;C] at the unit.</param>
        /// <returns>
        /// The selected mode, or <see cref="VentilationUnitOperatingMode.Undefined"/> where the strategy
        /// refuses or either temperature is not a finite number.
        /// </returns>
        public VentilationUnitOperatingMode OperatingMode(double intakeTemperature_C, double extractTemperature_C)
        {
            if (Refusal() is not null || !IsFinite(intakeTemperature_C) || !IsFinite(extractTemperature_C))
            {
                return VentilationUnitOperatingMode.Undefined;
            }

            if (extractTemperature_C > CoolingActivationTemperature_C)
            {
                return VentilationUnitOperatingMode.Cooling;
            }

            if (intakeTemperature_C > BypassMinimumIntakeTemperature_C
                && extractTemperature_C > intakeTemperature_C
                && extractTemperature_C > BypassMinimumExtractTemperature_C)
            {
                return VentilationUnitOperatingMode.SummerBypass;
            }

            return VentilationUnitOperatingMode.HeatCoolthRecovery;
        }

        /// <summary>The rule stated for one mode, or null where the mode has none.</summary>
        public SupplyTemperatureRule SupplyTemperatureRule(VentilationUnitOperatingMode ventilationUnitOperatingMode)
        {
            switch (ventilationUnitOperatingMode)
            {
                case VentilationUnitOperatingMode.SummerBypass:
                    return SummerBypassSupplyTemperatureRule;

                case VentilationUnitOperatingMode.HeatCoolthRecovery:
                    return HeatCoolthRecoverySupplyTemperatureRule;

                case VentilationUnitOperatingMode.Cooling:
                    return CoolingSupplyTemperatureRule;

                default:
                    return null;
            }
        }

        /// <summary>
        /// The total airflow [l/s] the unit moves in one mode, given what the dwelling was designed to move.
        /// <para>
        /// <b>The design airflow is read, never written.</b> The background modes move exactly it; cooling
        /// moves the stated elevated airflow. Nothing here alters the design figure, and the elevated figure
        /// never becomes one.
        /// </para>
        /// </summary>
        /// <param name="ventilationUnitOperatingMode">The mode the unit is in.</param>
        /// <param name="designAirFlowRate_Lps">What the dwelling is designed to move [l/s] - the background rate.</param>
        /// <returns>The operating airflow, or <see cref="double.NaN"/> where it cannot be stated.</returns>
        public double OperatingAirFlowRate_Lps(VentilationUnitOperatingMode ventilationUnitOperatingMode, double designAirFlowRate_Lps)
        {
            if (Refusal() is not null)
            {
                return double.NaN;
            }

            switch (ventilationUnitOperatingMode)
            {
                case VentilationUnitOperatingMode.SummerBypass:
                case VentilationUnitOperatingMode.HeatCoolthRecovery:
                    return IsFinite(designAirFlowRate_Lps) && designAirFlowRate_Lps > 0 ? designAirFlowRate_Lps : double.NaN;

                case VentilationUnitOperatingMode.Cooling:
                    return ElevatedAirFlow_Lps;

                default:
                    return double.NaN;
            }
        }

        /// <summary>
        /// The package supply temperature [&#176;C] the unit delivers for one hour, and the mode it is in
        /// while delivering it.
        /// </summary>
        /// <param name="intakeTemperature_C">The outdoor / intake air temperature [&#176;C] at the unit.</param>
        /// <param name="extractTemperature_C">The extract / return air temperature [&#176;C] at the unit.</param>
        /// <param name="designAirFlowRate_Lps">What the dwelling is designed to move [l/s] - the background rate.</param>
        /// <param name="ventilationUnitPerformanceTable">The product's published supply-air temperature table.</param>
        /// <param name="ventilationUnitOperatingMode">The mode selected.</param>
        /// <param name="operatingAirFlowRate_Lps">The airflow [l/s] the unit moves in that mode.</param>
        /// <returns>The supply temperature, or <see cref="double.NaN"/> where it cannot be stated.</returns>
        public double SupplyTemperature(
            double intakeTemperature_C,
            double extractTemperature_C,
            double designAirFlowRate_Lps,
            VentilationUnitPerformanceTable ventilationUnitPerformanceTable,
            out VentilationUnitOperatingMode ventilationUnitOperatingMode,
            out double operatingAirFlowRate_Lps)
        {
            ventilationUnitOperatingMode = OperatingMode(intakeTemperature_C, extractTemperature_C);
            operatingAirFlowRate_Lps = OperatingAirFlowRate_Lps(ventilationUnitOperatingMode, designAirFlowRate_Lps);

            SupplyTemperatureRule supplyTemperatureRule = SupplyTemperatureRule(ventilationUnitOperatingMode);

            return supplyTemperatureRule is null
                ? double.NaN
                : supplyTemperatureRule.SupplyTemperature(intakeTemperature_C, extractTemperature_C, operatingAirFlowRate_Lps, ventilationUnitPerformanceTable);
        }

        /// <summary>
        /// Why this strategy cannot be used, in words, or null where it can be - <b>including</b> the one
        /// figure only a project can state. This is the question every operating call asks.
        /// </summary>
        public string Refusal()
        {
            string result = TemplateRefusal();
            if (result is not null)
            {
                return result;
            }

            if (!IsResolved)
            {
                return "states no elevated cooling airflow for this dwelling, so what the unit moves while cooling is unresolved.";
            }

            return null;
        }

        /// <summary>
        /// Why this strategy is not a usable record of a manufacturer's guidance, in words, or null where it
        /// is. Every rule refuses rather than repairs - nothing here clamps a setpoint into range, fills in
        /// an absent threshold, or defaults a missing mode to another mode's behaviour.
        /// <para>
        /// <b>The elevated cooling airflow is not required here</b>, and that is the difference between this
        /// and <see cref="Refusal"/>. A catalogue states a manufacturer's thresholds, rules and ranges; how
        /// far up the elevated range one dwelling goes is that dwelling's decision, made where the design is
        /// known. A catalogue entry is therefore complete without it, and an operating call is not - see
        /// <see cref="IsResolved"/>. A figure that <i>is</i> stated is checked against the range here, so a
        /// catalogue cannot state an impossible one.
        /// </para>
        /// </summary>
        public string TemplateRefusal()
        {
            if (string.IsNullOrWhiteSpace(Source))
            {
                return "states no source, so its control logic cannot be traced to a manufacturer's own guidance.";
            }

            foreach ((string name, double value) in new[]
            {
                ("cooling activation temperature", CoolingActivationTemperature_C),
                ("lowest permitted cooling activation temperature", MinimumCoolingActivationTemperature_C),
                ("highest permitted cooling activation temperature", MaximumCoolingActivationTemperature_C),
                ("bypass minimum intake temperature", BypassMinimumIntakeTemperature_C),
                ("bypass minimum extract temperature", BypassMinimumExtractTemperature_C),
            })
            {
                if (!IsFinite(value))
                {
                    return string.Format("states no {0}.", name);
                }
            }

            if (MaximumCoolingActivationTemperature_C < MinimumCoolingActivationTemperature_C)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "states a permitted cooling activation range of {0:0.###} to {1:0.###} degC, which is not a range.",
                    MinimumCoolingActivationTemperature_C,
                    MaximumCoolingActivationTemperature_C);
            }

            if (CoolingActivationTemperature_C < MinimumCoolingActivationTemperature_C || CoolingActivationTemperature_C > MaximumCoolingActivationTemperature_C)
            {
                return string.Format(
                    CultureInfo.InvariantCulture,
                    "states a cooling activation temperature of {0:0.###} degC, outside the {1:0.###} to {2:0.###} degC its guidance permits.",
                    CoolingActivationTemperature_C,
                    MinimumCoolingActivationTemperature_C,
                    MaximumCoolingActivationTemperature_C);
            }

            if (IsFinite(ElevatedAirFlow_Lps) && ElevatedAirFlow_Lps <= 0)
            {
                return string.Format(CultureInfo.InvariantCulture, "states an elevated cooling airflow of {0} l/s.", ElevatedAirFlow_Lps);
            }

            if (double.IsInfinity(ElevatedAirFlow_Lps))
            {
                return "states an elevated cooling airflow that is not a finite number.";
            }

            //The advisory range is optional - a manufacturer may state a figure without stating a range -
            //but a range that is stated is enforced, and a half-stated range is a transcription mistake.
            bool hasMinimum = IsFinite(MinimumElevatedAirFlow_Lps);
            bool hasMaximum = IsFinite(MaximumElevatedAirFlow_Lps);

            if (hasMinimum != hasMaximum)
            {
                return "states one end of the elevated cooling airflow range without the other.";
            }

            if (hasMinimum)
            {
                if (MaximumElevatedAirFlow_Lps < MinimumElevatedAirFlow_Lps)
                {
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "states an elevated cooling airflow range of {0:0.###} to {1:0.###} l/s, which is not a range.",
                        MinimumElevatedAirFlow_Lps,
                        MaximumElevatedAirFlow_Lps);
                }

                if (IsResolved && (ElevatedAirFlow_Lps < MinimumElevatedAirFlow_Lps || ElevatedAirFlow_Lps > MaximumElevatedAirFlow_Lps))
                {
                    return string.Format(
                        CultureInfo.InvariantCulture,
                        "states an elevated cooling airflow of {0:0.###} l/s, outside the {1:0.###} to {2:0.###} l/s its guidance states.",
                        ElevatedAirFlow_Lps,
                        MinimumElevatedAirFlow_Lps,
                        MaximumElevatedAirFlow_Lps);
                }
            }

            foreach ((VentilationUnitOperatingMode mode, SupplyTemperatureRule rule) in new[]
            {
                (VentilationUnitOperatingMode.SummerBypass, SummerBypassSupplyTemperatureRule),
                (VentilationUnitOperatingMode.HeatCoolthRecovery, HeatCoolthRecoverySupplyTemperatureRule),
                (VentilationUnitOperatingMode.Cooling, CoolingSupplyTemperatureRule),
            })
            {
                if (rule is null)
                {
                    return string.Format("states no supply-temperature rule for its '{0}' mode.", Core.Query.Description(mode));
                }

                string refusal = rule.Refusal();
                if (refusal is not null)
                {
                    return string.Format("has a '{0}' mode that {1}", Core.Query.Description(mode), refusal);
                }
            }

            return null;
        }

        public override string ToString()
        {
            return TemplateRefusal() is not null
                ? "Invalid VentilationUnitOperatingStrategy"
                : string.Format(
                    CultureInfo.InvariantCulture,
                    "cooling above {0:0.###} degC at {1}; bypass above {2:0.###} degC intake and {3:0.###} degC extract; otherwise recovery",
                    CoolingActivationTemperature_C,
                    IsResolved ? string.Format(CultureInfo.InvariantCulture, "{0:0.###} l/s", ElevatedAirFlow_Lps) : "an elevated airflow nobody has resolved",
                    BypassMinimumIntakeTemperature_C,
                    BypassMinimumExtractTemperature_C);
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject is null)
            {
                return false;
            }

            Source = PerformanceJson.Text(jsonObject, "Source");
            CoolingActivationTemperature_C = PerformanceJson.Value(jsonObject, "CoolingActivationTemperature_C");
            MinimumCoolingActivationTemperature_C = PerformanceJson.Value(jsonObject, "MinimumCoolingActivationTemperature_C");
            MaximumCoolingActivationTemperature_C = PerformanceJson.Value(jsonObject, "MaximumCoolingActivationTemperature_C");
            BypassMinimumIntakeTemperature_C = PerformanceJson.Value(jsonObject, "BypassMinimumIntakeTemperature_C");
            BypassMinimumExtractTemperature_C = PerformanceJson.Value(jsonObject, "BypassMinimumExtractTemperature_C");
            ElevatedAirFlow_Lps = PerformanceJson.Value(jsonObject, "ElevatedAirFlow_Lps");
            MinimumElevatedAirFlow_Lps = PerformanceJson.Value(jsonObject, "MinimumElevatedAirFlow_Lps");
            MaximumElevatedAirFlow_Lps = PerformanceJson.Value(jsonObject, "MaximumElevatedAirFlow_Lps");

            SummerBypassSupplyTemperatureRule = Rule(jsonObject, "SummerBypassSupplyTemperatureRule");
            HeatCoolthRecoverySupplyTemperatureRule = Rule(jsonObject, "HeatCoolthRecoverySupplyTemperatureRule");
            CoolingSupplyTemperatureRule = Rule(jsonObject, "CoolingSupplyTemperatureRule");

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject result = new()
            {
                ["_type"] = Core.Query.FullTypeName(this)
            };

            PerformanceJson.SetText(result, "Source", Source);
            PerformanceJson.SetValue(result, "CoolingActivationTemperature_C", CoolingActivationTemperature_C);
            PerformanceJson.SetValue(result, "MinimumCoolingActivationTemperature_C", MinimumCoolingActivationTemperature_C);
            PerformanceJson.SetValue(result, "MaximumCoolingActivationTemperature_C", MaximumCoolingActivationTemperature_C);
            PerformanceJson.SetValue(result, "BypassMinimumIntakeTemperature_C", BypassMinimumIntakeTemperature_C);
            PerformanceJson.SetValue(result, "BypassMinimumExtractTemperature_C", BypassMinimumExtractTemperature_C);
            PerformanceJson.SetValue(result, "ElevatedAirFlow_Lps", ElevatedAirFlow_Lps);
            PerformanceJson.SetValue(result, "MinimumElevatedAirFlow_Lps", MinimumElevatedAirFlow_Lps);
            PerformanceJson.SetValue(result, "MaximumElevatedAirFlow_Lps", MaximumElevatedAirFlow_Lps);

            Set(result, "SummerBypassSupplyTemperatureRule", SummerBypassSupplyTemperatureRule);
            Set(result, "HeatCoolthRecoverySupplyTemperatureRule", HeatCoolthRecoverySupplyTemperatureRule);
            Set(result, "CoolingSupplyTemperatureRule", CoolingSupplyTemperatureRule);

            return result;

            static void Set(JsonObject jsonObject, string name, SupplyTemperatureRule supplyTemperatureRule)
            {
                if (supplyTemperatureRule is not null)
                {
                    jsonObject[name] = supplyTemperatureRule.ToJsonObject();
                }
            }
        }

        private static SupplyTemperatureRule Rule(JsonObject jsonObject, string name)
        {
            return jsonObject[name] is JsonObject jsonObject_Rule ? new SupplyTemperatureRule(jsonObject_Rule) : null;
        }

        private static SupplyTemperatureRule Copy(SupplyTemperatureRule supplyTemperatureRule)
        {
            return supplyTemperatureRule is null ? null : new SupplyTemperatureRule(supplyTemperatureRule);
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
