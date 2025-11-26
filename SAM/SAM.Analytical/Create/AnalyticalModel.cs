using SAM.Core;
using System;
using System.Collections.Generic;
using System.Linq;

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

        public static AnalyticalModel AnalyticalModel_ByApertureConstruction(this AnalyticalModel analyticalModel, ApertureConstruction apertureConstruction, IEnumerable<Aperture> apertures = null)
        {
            if(analyticalModel is null)
            {
                return null;
            }

            List<Aperture> apertures_Temp = apertures == null ? [] : [.. apertures];
            if(apertures_Temp is null || apertures_Temp.Count == 0)
            {
                apertures_Temp = analyticalModel.GetApertures();
            }

            if(apertures_Temp == null || apertures_Temp.Count == 0)
            {
                return new AnalyticalModel(analyticalModel);
            }

            if (analyticalModel?.AdjacencyCluster is not AdjacencyCluster adjacencyCluster)
            {
                return new AnalyticalModel(analyticalModel);
            }

            adjacencyCluster = new AdjacencyCluster(adjacencyCluster, true);

            foreach (Aperture aperture in apertures_Temp)
            {
                Aperture aperture_Temp = new(aperture, apertureConstruction);

                if (adjacencyCluster.GetAperture(aperture_Temp.Guid, out Panel panel_Temp) is null || panel_Temp is null)
                {
                    continue;
                }

                panel_Temp = Panel(panel_Temp);

                panel_Temp.RemoveAperture(aperture_Temp.Guid);
                panel_Temp.AddAperture(aperture_Temp);

                adjacencyCluster.AddObject(panel_Temp);
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

            caseDataCollection.Add(new ApertureConstructionCaseData(apertureConstruction));

            analyticalModel?.SetValue(AnalyticalModelParameter.CaseDataCollection, caseDataCollection);

            return result;
        }

        public static AnalyticalModel AnalyticalModel_ByOpening(this AnalyticalModel analyticalModel, 
            IEnumerable<double> openingAngles,
            IEnumerable<string> descriptions = null,
            IEnumerable<string> functions = null,
            IEnumerable<System.Drawing.Color> colors = null,
            IEnumerable<double> factors = null,
            IEnumerable<Profile> profiles = null,
            bool paneSizeOnly = true,
            IEnumerable <Aperture> apertures = null)
        {
            if (analyticalModel is null)
            {
                return null;
            }

            List<Aperture> apertures_Temp = apertures == null ? [] : [.. apertures];
            if (apertures_Temp is null || apertures_Temp.Count == 0)
            {
                apertures_Temp = analyticalModel.GetApertures();
            }

            AnalyticalModel result = new AnalyticalModel(analyticalModel);

            if (openingAngles == null || openingAngles.Count() == 0)
            {
                return result;
            }


            if (apertures_Temp == null || apertures_Temp.Count == 0)
            {
                return result;
            }

            if (analyticalModel?.AdjacencyCluster is not AdjacencyCluster adjacencyCluster)
            {
                return result;
            }

            adjacencyCluster = new AdjacencyCluster(adjacencyCluster, true);

            //List<Aperture> apertures_Result = [];
            //List<double> dischargeCoefficients_Result = [];
            //List<IOpeningProperties> openingProperties_Result = [];

            for (int i = 0; i < apertures_Temp.Count; i++)
            {
                Aperture aperture = apertures_Temp[i];

                Panel panel = adjacencyCluster.GetPanel(aperture);
                if (panel == null)
                {
                    continue;
                }

                Aperture aperture_Temp = panel.GetAperture(aperture.Guid);
                if (aperture_Temp == null)
                {
                    continue;
                }

                panel = Panel(panel);
                aperture_Temp = new Aperture(aperture_Temp);

                double openingAngle = openingAngles.Count() > i ? openingAngles.ElementAt(i) : openingAngles.Last();
                double width = paneSizeOnly ? aperture_Temp.GetWidth(AperturePart.Pane) : aperture_Temp.GetWidth();
                double height = paneSizeOnly ? aperture_Temp.GetHeight(AperturePart.Pane) : aperture_Temp.GetHeight();

                double factor = (factors != null && factors.Count() != 0) ? (factors.Count() > i ? factors.ElementAt(i) : factors.Last()) : double.NaN;

                PartOOpeningProperties partOOpeningProperties = new(width, height, openingAngle);

                double dischargeCoefficient = partOOpeningProperties.GetDischargeCoefficient();

                ISingleOpeningProperties singleOpeningProperties = null;
                if (profiles != null && profiles.Count() != 0)
                {
                    Profile profile = profiles.Count() > i ? profiles.ElementAt(i) : profiles.Last();
                    ProfileOpeningProperties profileOpeningProperties = new(partOOpeningProperties.GetDischargeCoefficient(), profile);
                    if (!double.IsNaN(factor))
                    {
                        profileOpeningProperties.Factor = factor;
                    }

                    singleOpeningProperties = profileOpeningProperties;
                }
                else
                {
                    if (!double.IsNaN(factor))
                    {
                        partOOpeningProperties.Factor = factor;
                    }

                    singleOpeningProperties = partOOpeningProperties;
                }

                if (descriptions != null && descriptions.Count() != 0)
                {
                    string description = descriptions.Count() > i ? descriptions.ElementAt(i) : descriptions.Last();
                    singleOpeningProperties.SetValue(OpeningPropertiesParameter.Description, description);
                }

                string function_Temp = "zdwno,0,19.00,21.00,99.00";
                if (functions != null && functions.Count() != 0)
                {
                    function_Temp = functions.Count() > i ? functions.ElementAt(i) : functions.Last();
                }
                singleOpeningProperties.SetValue(OpeningPropertiesParameter.Function, function_Temp);

                if (colors != null && colors.Count() != 0)
                {
                    System.Drawing.Color color = colors.Count() > i ? colors.ElementAt(i) : colors.Last();
                    aperture_Temp.SetValue(ApertureParameter.Color, color);
                }
                else
                {
                    aperture_Temp.SetValue(ApertureParameter.Color, Query.Color(ApertureType.Window, AperturePart.Pane, true));
                }

                aperture_Temp.AddSingleOpeningProperties(singleOpeningProperties);

                panel.RemoveAperture(aperture.Guid);
                if (panel.AddAperture(aperture_Temp))
                {
                    adjacencyCluster.AddObject(panel);
                    //apertures_Result.Add(aperture_Temp);
                    //dischargeCoefficients_Result.Add(singleOpeningProperties.GetDischargeCoefficient());
                    //openingProperties_Result.Add(singleOpeningProperties);
                }
            }


            result = new(analyticalModel, adjacencyCluster);

            if (!analyticalModel.TryGetValue(AnalyticalModelParameter.CaseDataCollection, out CaseDataCollection caseDataCollection))
            {
                caseDataCollection = [];
            }
            else
            {
                caseDataCollection = [.. caseDataCollection];
            }

            caseDataCollection.Add(new OpeningCaseData(openingAngles?.FirstOrDefault() ?? double.NaN));

            return result;
        }

        /// <summary>Try to find the ratio whose interval contains the given azimuth.</summary>
        private static bool TryGetRatio(Dictionary<Range<double>, Tuple<double, ApertureConstruction>> map, double azimuthDeg, out double ratio, out ApertureConstruction apertureConstruction)
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