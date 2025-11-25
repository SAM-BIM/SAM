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
    }
}