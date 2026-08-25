// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using SAM.Analytical.Enums;
using System;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Query
    {
        /// <summary>
        /// Whether a Part O iteration may carry the Approved Document F continuous mechanical airflows onto
        /// the model it is about to simulate, read from the ventilation strategy the assessed zones state.
        /// <para>
        /// <b>The strategy is authoritative, exactly as it is for the assessment criterion.</b> The same
        /// stated <c>NV</c> that makes <c>TMOverheatingCalculator</c> assess a dwelling against the
        /// natural-ventilation criterion is what says the dwelling has no continuous mechanical supply. It
        /// would be incoherent to believe it for one and not the other.
        /// </para>
        /// <para>
        /// <b>Only <c>NV</c> skips.</b> Every other strategy - including <c>UV</c>, which is not mechanical
        /// either but selects the corridor criterion rather than the natural-ventilation one - keeps the
        /// behaviour this preparation has always had, so nothing outside the natural-ventilation case
        /// changes. A model mixing <c>NV</c> with anything else is refused rather than resolved.
        /// </para>
        /// </summary>
        /// <param name="zones">The zones the iteration assesses. Their guids key the strategies.</param>
        /// <param name="dictionary_VentilationStrategy">
        /// The strategy each assessed zone states, by zone guid - the same dictionary
        /// <see cref="Create.OverheatingScenarios(IEnumerable{Zone}, PartOIteration, Dictionary{Guid, string}, out List{string})"/>
        /// is given, so the airflow decision and the assessment criterion can never read different answers.
        /// </param>
        /// <param name="diagnostic">
        /// Why the answer is what it is - the note for a skip, the refusal naming every zone and its stated
        /// strategy for a mixed model, or null where the long-standing application is simply kept.
        /// </param>
        public static PartOPartFAirflowApplication PartOPartFAirflowApplication(IEnumerable<Zone> zones, Dictionary<Guid, string> dictionary_VentilationStrategy, out string diagnostic)
        {
            diagnostic = null;

            List<Tuple<Zone, string>> stated = [];

            foreach (Zone zone in zones ?? [])
            {
                if (zone == null)
                {
                    continue;
                }

                string ventilationStrategy = null;
                dictionary_VentilationStrategy?.TryGetValue(zone.Guid, out ventilationStrategy);

                stated.Add(new Tuple<Zone, string>(zone, ventilationStrategy));
            }

            if (stated.Count == 0)
            {
                //No assessed zone means no stated strategy, and an absence is not a statement that the
                //dwelling is naturally ventilated. The behaviour this preparation has always had is kept,
                //so a model with no zones prepares exactly as it did before this gate existed.
                return Enums.PartOPartFAirflowApplication.Apply;
            }

            List<Zone> zones_NaturalVentilation = [];
            List<Tuple<Zone, string>> zones_Other = [];

            foreach (Tuple<Zone, string> tuple in stated)
            {
                if (IsPartONaturalVentilation(tuple.Item2))
                {
                    zones_NaturalVentilation.Add(tuple.Item1);
                }
                else
                {
                    zones_Other.Add(tuple);
                }
            }

            if (zones_NaturalVentilation.Count == 0)
            {
                return Enums.PartOPartFAirflowApplication.Apply;
            }

            if (zones_Other.Count == 0)
            {
                diagnostic = string.Format(
                    "Approved Document F continuous mechanical airflow was NOT applied: every assessed zone states Natural Ventilation ({0}), so the dwelling has no continuous mechanical supply or extract to carry onto its internal conditions. The Part F sizing is System 4 shaped - paragraph 1.67 gives every habitable room a mechanical supply terminal regardless of how the dwelling is ventilated - and writing those rates here would simulate an MVHR system the building does not have. This means no mechanical system was invented. It does NOT mean the dwelling's natural-ventilation Part F design has been sized: background ventilator and purge provision under System 1 are not calculated by this preparation.",
                    string.Join(", ", zones_NaturalVentilation.ConvertAll(x => string.Format("'{0}'", x.Name))));

                return Enums.PartOPartFAirflowApplication.SkipNaturalVentilation;
            }

            //Neither answer is safe. Applying would put continuous mechanical supply and extract into the
            //naturally ventilated zones; skipping would strip the mechanically ventilated ones of the rates
            //they are sized for. ApplyPartFVentilationRates is whole-model, so there is no third option to
            //reach for here - making it zone-scoped is a separate change with its own transfer-air and
            //balance consequences.
            List<string> descriptions = [];

            foreach (Zone zone in zones_NaturalVentilation)
            {
                descriptions.Add(string.Format("'{0}' states NV", zone.Name));
            }

            foreach (Tuple<Zone, string> tuple in zones_Other)
            {
                descriptions.Add(string.Format("'{0}' states {1}", tuple.Item1.Name, string.IsNullOrWhiteSpace(tuple.Item2) ? "no strategy" : tuple.Item2));
            }

            diagnostic = string.Format(
                "The assessed zones mix Natural Ventilation with other ventilation strategies, so this iteration was not prepared: {0}. Approved Document F airflow is applied to the whole model at once, so there is no answer here that is right for both halves - applying would put continuous mechanical supply and extract into the naturally ventilated zones, and skipping would strip the rates the mechanically ventilated zones are sized for. Prepare the naturally ventilated zones and the mechanically ventilated zones as separate iterations, or wait for the per-zone application.",
                string.Join(", ", descriptions));

            return Enums.PartOPartFAirflowApplication.RefuseMixed;
        }

        /// <summary>
        /// Whether a stated ventilation strategy is the natural-ventilation one, read the same way
        /// <see cref="VentilationStrategyMap"/> reads it.
        /// <para>
        /// Upper-cased with <c>ToUpperInvariant</c> and trimmed, because the strategy arrives here straight
        /// off a Grasshopper text panel rather than through <c>SystemTemplate</c>'s space-stripping setters,
        /// and the current culture's casing rules would otherwise map <c>"nv"</c> to something that is not
        /// <c>"NV"</c>.
        /// </para>
        /// </summary>
        private static bool IsPartONaturalVentilation(string ventilationStrategy)
        {
            return !string.IsNullOrWhiteSpace(ventilationStrategy) && ventilationStrategy.Trim().ToUpperInvariant() == "NV";
        }
    }
}
