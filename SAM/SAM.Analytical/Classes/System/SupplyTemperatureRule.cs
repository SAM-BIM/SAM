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
    /// What air temperature a ventilation unit delivers into the dwelling in one of its operating modes,
    /// as its manufacturer states it - a rule expressed as data rather than as code.
    /// <para>
    /// <b>The quantity is the package supply temperature.</b> It is the air leaving the whole unit: every
    /// component inside the casing, in the arrangement the manufacturer built, has already acted on it.
    /// That is the number a manufacturer publishes and the number that can be observed downstream of the
    /// unit in a simulation, which is why it - and not any internal component property - is what this
    /// vocabulary carries and what an implementation is held to.
    /// </para>
    /// <para>
    /// <b><see cref="ExtractFraction"/> is not a certified heat-recovery efficiency and must never be
    /// written as one.</b> A manufacturer's simplified modelling guidance may state that its unit delivers,
    /// say, four fifths of the way from intake to extract temperature. A certified EN 13141-7 / SAP
    /// temperature ratio is a measured property of a specified test, on a stated basis, for a stated
    /// product configuration. The two answer different questions and are traceable to different documents,
    /// so a blend fraction stated here is carried here, is reported as manufacturer modelling guidance, and
    /// is never copied into <see cref="HeatRecoveryPerformance"/> - which exists for the certified figure
    /// and is what <c>Query.VentilationUnitOperatingParameters</c> requires. Numerical similarity between
    /// the two is not evidence that either is the other.
    /// </para>
    /// <para>
    /// <b>A rule of type <see cref="SupplyTemperatureRuleType.PerformanceTable"/> carries no table.</b> The
    /// product's published data already lives on its <see cref="VentilationUnitTemplate"/>; a copy here
    /// would be a second place for it to be wrong. The rule says "read the published table" and whoever
    /// resolves the strategy hands it in - see <see cref="SupplyTemperature(double, double, double,
    /// VentilationUnitPerformanceTable)"/>.
    /// </para>
    /// </summary>
    public class SupplyTemperatureRule : IJSAMObject
    {
        private SupplyTemperatureRuleType supplyTemperatureRuleType = SupplyTemperatureRuleType.Undefined;
        private double extractFraction = double.NaN;
        private double minimumSupplyTemperature_C = double.NaN;
        private PerformanceDomainPolicy performanceDomainPolicy = PerformanceDomainPolicy.ClampToDomain;
        private bool performanceDomainPolicy_Refused = false;

        public SupplyTemperatureRule()
        {
        }

        /// <summary>The rule for a mode that delivers intake air unchanged.</summary>
        public static SupplyTemperatureRule OutdoorAir()
        {
            return new SupplyTemperatureRule { supplyTemperatureRuleType = SupplyTemperatureRuleType.OutdoorAir };
        }

        /// <summary>
        /// The rule for a mode that delivers a stated blend of extract and intake air -
        /// <c>extractFraction * extract + (1 - extractFraction) * intake</c>. Read the type remarks before
        /// supplying a fraction taken from a certified document rather than from modelling guidance.
        /// </summary>
        public static SupplyTemperatureRule LinearBlend(double extractFraction)
        {
            return new SupplyTemperatureRule
            {
                supplyTemperatureRuleType = SupplyTemperatureRuleType.LinearBlend,
                extractFraction = extractFraction,
            };
        }

        /// <summary>
        /// The rule for a mode whose supply temperature the manufacturer publishes as a table, optionally
        /// with a stated lower limit.
        /// </summary>
        /// <param name="minimumSupplyTemperature_C">
        /// The lowest supply temperature [&#176;C] the manufacturer's guidance allows, or
        /// <see cref="double.NaN"/> where it states none. Applied to the table's answer, never to the
        /// table's own published cells - a published figure stays exactly what was published, and a limit
        /// stated by guidance is applied by the guidance route that states it.
        /// </param>
        /// <param name="performanceDomainPolicy">
        /// What the lookup does outside the published grid. Defaults to
        /// <see cref="PerformanceDomainPolicy.ClampToDomain"/> - holding at the published edges, which is
        /// what the already-accepted cooling route does, and is necessary because a cooling table published
        /// over summer design conditions is asked about every hour of the year.
        /// </param>
        public static SupplyTemperatureRule PerformanceTable(double minimumSupplyTemperature_C = double.NaN, PerformanceDomainPolicy performanceDomainPolicy = PerformanceDomainPolicy.ClampToDomain)
        {
            return new SupplyTemperatureRule
            {
                supplyTemperatureRuleType = SupplyTemperatureRuleType.PerformanceTable,
                minimumSupplyTemperature_C = minimumSupplyTemperature_C,
                performanceDomainPolicy = performanceDomainPolicy,
            };
        }

        public SupplyTemperatureRule(SupplyTemperatureRule supplyTemperatureRule)
        {
            if (supplyTemperatureRule is not null)
            {
                supplyTemperatureRuleType = supplyTemperatureRule.supplyTemperatureRuleType;
                extractFraction = supplyTemperatureRule.extractFraction;
                minimumSupplyTemperature_C = supplyTemperatureRule.minimumSupplyTemperature_C;
                performanceDomainPolicy = supplyTemperatureRule.performanceDomainPolicy;
                performanceDomainPolicy_Refused = supplyTemperatureRule.performanceDomainPolicy_Refused;
            }
        }

        public SupplyTemperatureRule(JsonObject jsonObject)
        {
            FromJsonObject(jsonObject);
        }

        /// <summary>How this mode's supply temperature is stated.</summary>
        public SupplyTemperatureRuleType SupplyTemperatureRuleType
        {
            get
            {
                return supplyTemperatureRuleType;
            }
        }

        /// <summary>
        /// The share [-] of the way from intake to extract temperature this mode delivers, for a
        /// <see cref="SupplyTemperatureRuleType.LinearBlend"/> rule; <see cref="double.NaN"/> otherwise.
        /// <b>Not a certified heat-recovery efficiency</b> - see the type remarks.
        /// </summary>
        public double ExtractFraction
        {
            get
            {
                return extractFraction;
            }
        }

        /// <summary>
        /// The lowest supply temperature [&#176;C] the rule allows, or <see cref="double.NaN"/> where none
        /// is stated. Applied to what the rule computes, not to any published cell.
        /// </summary>
        public double MinimumSupplyTemperature_C
        {
            get
            {
                return minimumSupplyTemperature_C;
            }
        }

        /// <summary>What a table lookup does outside the published grid.</summary>
        public PerformanceDomainPolicy PerformanceDomainPolicy
        {
            get
            {
                return performanceDomainPolicy;
            }
        }

        /// <summary>
        /// Why this rule cannot be used, in words, or null where it can be.
        /// <para>
        /// Every rule is a refusal rather than a repair: a fraction outside the unit interval, a
        /// non-finite limit or an unrecognised policy name is a transcription mistake, and the one
        /// direction a transcription mistake must never take is "close enough".
        /// </para>
        /// </summary>
        public string Refusal()
        {
            if (performanceDomainPolicy_Refused)
            {
                return "states a performance domain policy that is not one of the stated names.";
            }

            switch (supplyTemperatureRuleType)
            {
                case SupplyTemperatureRuleType.Undefined:
                    return "states no supply-temperature rule.";

                case SupplyTemperatureRuleType.OutdoorAir:
                    break;

                case SupplyTemperatureRuleType.LinearBlend:
                    if (double.IsNaN(extractFraction) || double.IsInfinity(extractFraction))
                    {
                        return "states a blend of extract and intake air without stating the extract fraction.";
                    }

                    if (extractFraction < 0 || extractFraction > 1)
                    {
                        return string.Format(CultureInfo.InvariantCulture, "states an extract fraction of {0}; a blend of two air streams lies between 0 and 1.", extractFraction);
                    }

                    break;

                case SupplyTemperatureRuleType.PerformanceTable:
                    break;

                default:
                    return string.Format("states the unrecognised supply-temperature rule '{0}'.", supplyTemperatureRuleType);
            }

            if (double.IsInfinity(minimumSupplyTemperature_C))
            {
                return "states a minimum supply temperature that is not a finite number.";
            }

            return null;
        }

        /// <summary>
        /// The package supply temperature [&#176;C] this rule states, for one hour's intake and extract air
        /// temperatures and the airflow the unit is moving.
        /// </summary>
        /// <param name="intakeTemperature_C">The outdoor / intake air temperature [&#176;C] at the unit.</param>
        /// <param name="extractTemperature_C">The extract / return air temperature [&#176;C] at the unit.</param>
        /// <param name="airFlowRate_Lps">The airflow [l/s] the unit is moving in this mode.</param>
        /// <param name="ventilationUnitPerformanceTable">
        /// The product's published table, required only by a
        /// <see cref="SupplyTemperatureRuleType.PerformanceTable"/> rule and ignored by the others.
        /// </param>
        /// <returns>
        /// The supply temperature, or <see cref="double.NaN"/> where the rule refuses, an input is not a
        /// finite number, or the table cannot answer.
        /// </returns>
        public double SupplyTemperature(double intakeTemperature_C, double extractTemperature_C, double airFlowRate_Lps, VentilationUnitPerformanceTable ventilationUnitPerformanceTable = null)
        {
            if (Refusal() is not null || !IsFinite(intakeTemperature_C) || !IsFinite(extractTemperature_C))
            {
                return double.NaN;
            }

            double result;

            switch (supplyTemperatureRuleType)
            {
                case SupplyTemperatureRuleType.OutdoorAir:
                    result = intakeTemperature_C;
                    break;

                case SupplyTemperatureRuleType.LinearBlend:
                    result = (extractFraction * extractTemperature_C) + ((1 - extractFraction) * intakeTemperature_C);
                    break;

                case SupplyTemperatureRuleType.PerformanceTable:
                    if (ventilationUnitPerformanceTable is null || !IsFinite(airFlowRate_Lps))
                    {
                        return double.NaN;
                    }

                    result = ventilationUnitPerformanceTable.Value(
                        VentilationUnitPerformanceOutput.Name_SupplyAirTemperature,
                        TableArguments(ventilationUnitPerformanceTable, intakeTemperature_C, extractTemperature_C, airFlowRate_Lps),
                        performanceDomainPolicy);

                    break;

                default:
                    return double.NaN;
            }

            if (!IsFinite(result))
            {
                return double.NaN;
            }

            //A stated lower limit is applied to what the rule computes. It never reaches back into a
            //published cell: the published table stays exactly what the manufacturer printed, and a limit
            //its guidance states is applied by the guidance route that states it.
            return double.IsNaN(minimumSupplyTemperature_C) ? result : System.Math.Max(result, minimumSupplyTemperature_C);
        }

        /// <summary>
        /// The arguments of a published supply-air temperature table, in the order that table states its
        /// axes.
        /// <para>
        /// <b>By axis name, never by position.</b> The table's axes are its own; reading them positionally
        /// would silently swap an intake temperature for an extract temperature the first time a catalogue
        /// wrote them the other way round.
        /// </para>
        /// </summary>
        private static double[] TableArguments(VentilationUnitPerformanceTable ventilationUnitPerformanceTable, double intakeTemperature_C, double extractTemperature_C, double airFlowRate_Lps)
        {
            double[] result = new double[ventilationUnitPerformanceTable.AxisCount];

            for (int i = 0; i < result.Length; i++)
            {
                result[i] = double.NaN;
            }

            Set(VentilationUnitPerformanceAxis.Name_ExternalDryBulbTemperature, intakeTemperature_C);
            Set(VentilationUnitPerformanceAxis.Name_EnteringDryBulbTemperature, extractTemperature_C);
            Set(VentilationUnitPerformanceAxis.Name_AirFlowRate, airFlowRate_Lps);

            return result;

            void Set(string name, double value)
            {
                int index = ventilationUnitPerformanceTable.AxisIndex(name);
                if (index >= 0 && index < result.Length)
                {
                    result[index] = value;
                }
            }
        }

        public override string ToString()
        {
            string result;

            switch (supplyTemperatureRuleType)
            {
                case SupplyTemperatureRuleType.OutdoorAir:
                    result = "intake air";
                    break;

                case SupplyTemperatureRuleType.LinearBlend:
                    result = string.Format(CultureInfo.InvariantCulture, "{0:0.###} x extract + {1:0.###} x intake", extractFraction, 1 - extractFraction);
                    break;

                case SupplyTemperatureRuleType.PerformanceTable:
                    result = string.Format("published table ({0} outside)", performanceDomainPolicy);
                    break;

                default:
                    return "Invalid SupplyTemperatureRule";
            }

            return double.IsNaN(minimumSupplyTemperature_C)
                ? result
                : string.Format(CultureInfo.InvariantCulture, "{0}, not below {1:0.###} degC", result, minimumSupplyTemperature_C);
        }

        public bool FromJsonObject(JsonObject jsonObject)
        {
            if (jsonObject is null)
            {
                return false;
            }

            supplyTemperatureRuleType = Core.Query.Enum<SupplyTemperatureRuleType>(PerformanceJson.Text(jsonObject, "SupplyTemperatureRuleType"));
            extractFraction = PerformanceJson.Value(jsonObject, "ExtractFraction");
            minimumSupplyTemperature_C = PerformanceJson.Value(jsonObject, "MinimumSupplyTemperature_C");

            //Absent reads as ClampToDomain, which is what a published table asked about conditions outside
            //its grid already does on the accepted cooling route. PRESENT but not one of the names is
            //refused rather than quietly clamped - see FlowFractionControlCurve, which takes the same
            //position for the same reason.
            performanceDomainPolicy = PerformanceDomainPolicy.ClampToDomain;
            performanceDomainPolicy_Refused = false;

            string text = PerformanceJson.Text(jsonObject, "PerformanceDomainPolicy");

            if (!string.IsNullOrWhiteSpace(text))
            {
                PerformanceDomainPolicy performanceDomainPolicy_Temp = Core.Query.Enum<PerformanceDomainPolicy>(text);

                if (performanceDomainPolicy_Temp == PerformanceDomainPolicy.Undefined)
                {
                    performanceDomainPolicy_Refused = true;
                }
                else
                {
                    performanceDomainPolicy = performanceDomainPolicy_Temp;
                }
            }

            return true;
        }

        public JsonObject ToJsonObject()
        {
            JsonObject result = new()
            {
                ["_type"] = Core.Query.FullTypeName(this),
                ["SupplyTemperatureRuleType"] = supplyTemperatureRuleType.ToString(),
            };

            PerformanceJson.SetValue(result, "ExtractFraction", extractFraction);
            PerformanceJson.SetValue(result, "MinimumSupplyTemperature_C", minimumSupplyTemperature_C);

            result["PerformanceDomainPolicy"] = performanceDomainPolicy.ToString();

            return result;
        }

        private static bool IsFinite(double value)
        {
            return !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
