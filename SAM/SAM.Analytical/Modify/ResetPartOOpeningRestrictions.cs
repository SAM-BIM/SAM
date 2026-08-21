// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Modify
    {
        /// <summary>
        /// Resets every SAM-generated <see cref="PartOOpeningProperties.OpeningRestriction"/> on the model
        /// to <see cref="OpeningRestriction.Unrestricted"/>, on a copy - the original
        /// <paramref name="analyticalModel"/> is never modified.
        /// <para>
        /// This is what makes a <c>BasePassive</c> ("openings operated without restriction") iteration
        /// enforce its own assumption on the copy that gets simulated, rather than merely stating it on the
        /// scenario. Only <see cref="PartOOpeningProperties"/> is touched - any other
        /// <see cref="IOpeningProperties"/> (a plain <see cref="OpeningProperties"/>, a
        /// <see cref="ProfileOpeningProperties"/>, or anything else not produced by SAM's own Part O
        /// authoring) is left exactly as found, because it is not this method's data to change.
        /// </para>
        /// </summary>
        /// <param name="analyticalModel">The model to reset a copy of.</param>
        /// <param name="notes">Which apertures were reset, for reporting. Never null.</param>
        /// <returns>
        /// A copy with every SAM-generated restriction reset, or <paramref name="analyticalModel"/> itself
        /// (still un-mutated) when nothing needed resetting.
        /// </returns>
        public static AnalyticalModel ResetPartOOpeningRestrictions(this AnalyticalModel analyticalModel, out List<string> notes)
        {
            notes = new List<string>();

            if (analyticalModel?.AdjacencyCluster is not AdjacencyCluster adjacencyCluster)
            {
                return analyticalModel;
            }

            List<Panel> panels = adjacencyCluster.GetPanels();
            if (panels == null || panels.Count == 0)
            {
                return analyticalModel;
            }

            AdjacencyCluster adjacencyCluster_Copy = null;

            foreach (Panel panel in panels)
            {
                List<Aperture> apertures = panel?.Apertures;
                if (apertures == null || apertures.Count == 0)
                {
                    continue;
                }

                Panel panel_Updated = null;

                foreach (Aperture aperture in apertures)
                {
                    if (!aperture.TryGetValue(ApertureParameter.OpeningProperties, out IOpeningProperties openingProperties) || openingProperties == null)
                    {
                        continue;
                    }

                    IOpeningProperties openingProperties_Reset = ResetOpeningRestriction(openingProperties, out bool changed);
                    if (!changed)
                    {
                        continue;
                    }

                    if (panel_Updated == null)
                    {
                        panel_Updated = Create.Panel(panel);
                    }

                    Aperture aperture_Updated = new Aperture(aperture);
                    aperture_Updated.SetValue(ApertureParameter.OpeningProperties, openingProperties_Reset);
                    panel_Updated.RemoveAperture(aperture.Guid);
                    panel_Updated.AddAperture(aperture_Updated);

                    notes.Add(string.Format("Reset Part O opening restriction to Unrestricted on aperture '{0}' (panel '{1}').", aperture.Name, panel.Name));
                }

                if (panel_Updated != null)
                {
                    if (adjacencyCluster_Copy == null)
                    {
                        adjacencyCluster_Copy = new AdjacencyCluster(adjacencyCluster, true);
                    }

                    adjacencyCluster_Copy.AddObject(panel_Updated);
                }
            }

            if (adjacencyCluster_Copy == null)
            {
                return analyticalModel;
            }

            return new AnalyticalModel(analyticalModel, adjacencyCluster_Copy);
        }

        private static IOpeningProperties ResetOpeningRestriction(IOpeningProperties openingProperties, out bool changed)
        {
            changed = false;

            if (openingProperties is PartOOpeningProperties partOOpeningProperties)
            {
                if (partOOpeningProperties.OpeningRestriction == OpeningRestriction.Unrestricted)
                {
                    return openingProperties;
                }

                changed = true;
                return new PartOOpeningProperties(partOOpeningProperties) { OpeningRestriction = OpeningRestriction.Unrestricted };
            }

            if (openingProperties is MultipleOpeningProperties multipleOpeningProperties)
            {
                List<ISingleOpeningProperties> singleOpeningProperties = multipleOpeningProperties.SingleOpeningProperties;
                if (singleOpeningProperties == null || singleOpeningProperties.Count == 0)
                {
                    return openingProperties;
                }

                bool any = false;
                List<ISingleOpeningProperties> result = new List<ISingleOpeningProperties>();
                foreach (ISingleOpeningProperties singleOpeningProperties_Item in singleOpeningProperties)
                {
                    IOpeningProperties reset_Item = ResetOpeningRestriction(singleOpeningProperties_Item, out bool changed_Item);
                    if (changed_Item)
                    {
                        any = true;
                    }

                    result.Add((ISingleOpeningProperties)reset_Item);
                }

                if (!any)
                {
                    return openingProperties;
                }

                changed = true;
                return new MultipleOpeningProperties(multipleOpeningProperties, result);
            }

            return openingProperties;
        }
    }
}
