using SAM.Core;
using System;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Create
    {
        public static AnalyticalModel AnalyticalModel_ByWindowSize(this AnalyticalModel analyticalModel, double apertureScaleFactor, IEnumerable<Aperture> apertures = null)
        {
            if(analyticalModel == null || double.IsNaN(apertureScaleFactor))
            {
                return null;
            }

            List<Aperture> apertures_Temp = apertures == null ? [] : [.. apertures];


            // Resolve aperture list from model when not provided
            if (apertures_Temp.Count == 0)
            {
                apertures = analyticalModel.GetApertures();
            }
            else
            {
                // Clean input list and re-fetch from model to ensure consistency
                for (int i = apertures_Temp.Count - 1; i >= 0; i--)
                {
                    if (apertures_Temp[i] is null)
                    {
                        apertures_Temp.RemoveAt(i);
                        continue;
                    }

                    Aperture aperture = analyticalModel.GetAperture(apertures_Temp[i].Guid, out Panel panel);
                    if (aperture is null)
                    {
                        apertures_Temp.RemoveAt(i);
                        continue;
                    }

                    apertures_Temp[i] = new Aperture(aperture);
                }
            }

            // Apply scaling when we have targets and a valid factor
            if (apertures_Temp == null || apertures_Temp.Count == 0)
            {
                return new AnalyticalModel(analyticalModel);
            }

            AdjacencyCluster adjacencyCluster = new(analyticalModel.AdjacencyCluster, true);

            foreach (Aperture aperture in apertures)
            {
                Aperture aperture_Temp = aperture.Rescale(apertureScaleFactor);
                if (aperture_Temp is null)
                {
                    continue;
                }

                if (adjacencyCluster.GetAperture(aperture_Temp.Guid, out Panel panel_Temp) is null || panel_Temp is null)
                {
                    continue;
                }

                panel_Temp = Panel(panel_Temp);

                panel_Temp.RemoveAperture(aperture_Temp.Guid);
                panel_Temp.AddAperture(aperture_Temp);

                adjacencyCluster.AddObject(panel_Temp);
            }

            AnalyticalModel result = new (analyticalModel, adjacencyCluster);

            if (!result.TryGetValue(AnalyticalModelParameter.CaseDataCollection, out CaseDataCollection caseDataCollection))
            {
                caseDataCollection = [];
            }
            else
            {
                caseDataCollection = [.. caseDataCollection];
            }

            caseDataCollection.Add(new WindowSizeCaseData(apertureScaleFactor));

            result?.SetValue(AnalyticalModelParameter.CaseDataCollection, caseDataCollection);

            return result;
        }
    
        public static AnalyticalModel AnalyticalModel_ByApertureByAzimuths(this AnalyticalModel analyticalModel, Dictionary<Range<double>, Tuple<double, ApertureConstruction>> intervalRatioMap, bool subdivide, double apertureHeight, double sillHeight, double horizontalSeparation, double offset, bool keepSeparationDistance, IEnumerable<Panel> panels = null)
        {
            if(analyticalModel == null)
            {
                return null;
            }

            if(intervalRatioMap == null || intervalRatioMap.Count == 0)
            {
                return new AnalyticalModel(analyticalModel);
            }

            if (analyticalModel?.AdjacencyCluster is not AdjacencyCluster adjacencyCluster)
            {
                return new AnalyticalModel(analyticalModel);
            }

            adjacencyCluster = new AdjacencyCluster(adjacencyCluster, true);

            List<Panel> panels_Temp = panels == null ? [] : [.. panels];

            if(panels_Temp == null || panels_Temp.Count == 0)
            {
                panels_Temp = analyticalModel.GetPanels();
            }

            HashSet<double> ratios = [];

            foreach (Panel panel in panels)
            {
                if (panel.PanelType != PanelType.WallExternal)
                {
                    continue;
                }

                double az = NormalizeAngleDegrees(panel.Azimuth());
                if (double.IsNaN(az))
                {
                    continue;
                }

                if (!TryGetRatio(intervalRatioMap, az, out double ratio, out ApertureConstruction apertureConstruction_Temp))
                {
                    continue;
                }

                ratios.Add(ratio);

                Panel panel_New = Panel(panel);

                if (apertureConstruction_Temp is null)
                {
                    apertureConstruction_Temp = Query.DefaultApertureConstruction(panel_New, ApertureType.Window);
                }

                List<Aperture> apertures = panel_New.AddApertures(apertureConstruction_Temp, ratio, subdivide, apertureHeight, sillHeight, horizontalSeparation, offset, keepSeparationDistance);
                if (apertures == null || apertures.Count == 0)
                {
                    continue;
                }

                adjacencyCluster.AddObject(panel_New);
            }

            AnalyticalModel result = new(analyticalModel, adjacencyCluster);

            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.CaseDataCollection, out CaseDataCollection caseDataCollection))
            {
                caseDataCollection = [];
            }
            else
            {
                caseDataCollection = [.. caseDataCollection];
            }

            caseDataCollection.Add(new ApertureCaseData(ratios));

            analyticalModel?.SetValue(AnalyticalModelParameter.CaseDataCollection, caseDataCollection);

            return result;
        }

        /// <summary>Try to find the ratio whose interval contains the given azimuth.</summary>
        private static bool TryGetRatio(
            Dictionary<Range<double>, Tuple<double, ApertureConstruction>> map,
            double azimuthDeg,
            out double ratio,
            out ApertureConstruction apertureConstruction)
        {
            double azimuthDeg_Round = System.Math.Round(azimuthDeg, MidpointRounding.ToEven);
            apertureConstruction = null;
            ratio = 0.0;

            foreach (var kvp in map)
            {
                if (kvp.Key.In(azimuthDeg_Round))
                {
                    ratio = kvp.Value.Item1;
                    apertureConstruction = kvp.Value.Item2;
                    return true;
                }
            }
            return false;
        }

        /// <summary>Normalise angle to [0, 359].</summary>
        private static double NormalizeAngleDegrees(double angleDeg)
        {
            if (double.IsNaN(angleDeg) || double.IsInfinity(angleDeg))
            {
                return double.NaN;
            }

            double a = angleDeg % 360.0;

            if (a < 0)
            {
                a += 360.0;
            }
            
            return (a >= 360.0) ? 359.0 : a;
        }

    }
}