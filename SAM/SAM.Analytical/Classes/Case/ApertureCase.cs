using Newtonsoft.Json.Linq;

namespace SAM.Analytical.Classes
{
    public class ApertureCase : Case
    {
        private ApertureToPanelRatios apertureToPanelRatios;
        private bool subdivide;
        private double apertureHeight;
        private double sillHeight;
        private double horizontalSeparation;
        private double offset;
        private bool keepSeparationDistance;
        private CaseSelection caseSelection;

        public ApertureCase()
            : base()
        {

        }

        public ApertureCase(JObject jObject)
            : base(jObject)
        {

        }

        public ApertureCase(ApertureCase apertureCase)
            : base(apertureCase)
        {
            if(apertureCase != null)
            {
                apertureToPanelRatios = apertureCase.apertureToPanelRatios;
                subdivide = apertureCase.subdivide;
                apertureHeight = apertureCase.apertureHeight;
                sillHeight = apertureCase.sillHeight;
                horizontalSeparation = apertureCase.horizontalSeparation;
                offset = apertureCase.offset;
                keepSeparationDistance = apertureCase.keepSeparationDistance;
                caseSelection = apertureCase.caseSelection;
            }
        }

        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);
            if (!result)
            {
                return false;
            }

            if (jObject.ContainsKey("ApertureToPanelRatios"))
            {
                apertureToPanelRatios = Core.Query.IJSAMObject<ApertureToPanelRatios>(jObject.Value<JObject>("ApertureToPanelRatios"));
            }

            if(jObject.ContainsKey("Subdivide"))
            {
                subdivide = jObject.Value<bool>("Subdivide");
            }

            if (jObject.ContainsKey("ApertureHeight"))
            {
                apertureHeight = jObject.Value<double>("ApertureHeight");
            }

            if (jObject.ContainsKey("SillHeight"))
            {
                sillHeight = jObject.Value<double>("SillHeight");
            }

            if (jObject.ContainsKey("HorizontalSeparation"))
            {
                horizontalSeparation = jObject.Value<double>("HorizontalSeparation");
            }

            if (jObject.ContainsKey("Offset"))
            {
                offset = jObject.Value<double>("Offset");
            }

            if (jObject.ContainsKey("CaseSelection"))
            {
                caseSelection = Core.Query.IJSAMObject<CaseSelection>(jObject.Value<JObject>("CaseSelection"));
            }

            return true;
        }

        public override JObject ToJObject()
        {
            JObject result = base.ToJObject();
            if (result is null)
            {
                return result;
            }

            if (apertureToPanelRatios != null)
            {
                result.Add("ApertureToPanelRatios", apertureToPanelRatios.ToJObject());
            }

            result.Add("Subdivide", subdivide);
            
            if (!double.IsNaN(apertureHeight))
            {
                result.Add("ApertureHeight", apertureHeight);
            }

            if (!double.IsNaN(sillHeight))
            {
                result.Add("SillHeight", sillHeight);
            }

            if (!double.IsNaN(horizontalSeparation))
            {
                result.Add("HorizontalSeparation", horizontalSeparation);
            }

            if (!double.IsNaN(offset))
            {
                result.Add("Offset", offset);
            }

            result.Add("KeepSeparationDistance", keepSeparationDistance);

            if (caseSelection != null)
            {
                result.Add("CaseSelection", caseSelection.ToJObject());
                result.Add("CaseSelection", caseSelection.ToJObject());
            }

            return result;
        }
    }
}
