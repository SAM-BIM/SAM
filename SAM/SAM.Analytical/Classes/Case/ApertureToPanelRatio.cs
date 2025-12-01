using Newtonsoft.Json.Linq;
using SAM.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace SAM.Analytical.Classes
{
    public class ApertureToPanelRatio : IJSAMObject
    {
        private Range<double> azimuthRange;
        private double ratio;
        private ApertureConstruction apertureConstruction;

        public ApertureToPanelRatio(Range<double> azimuthRange, double ratio, ApertureConstruction apertureConstruction)
        {
            this.azimuthRange = azimuthRange;
            this.ratio = ratio;
            this.apertureConstruction = apertureConstruction;
        }

        public ApertureToPanelRatio(JObject jObject)
        {
            FromJObject(jObject);
        }

        public ApertureToPanelRatio(ApertureToPanelRatio apertureToPanelRatio)
        {
            if (apertureToPanelRatio != null)
            {
                azimuthRange = apertureToPanelRatio.AzimuthRange;
                ratio = apertureToPanelRatio.Ratio;
                apertureConstruction = apertureToPanelRatio.ApertureConstruction;
            }
        }

        public Range<double> AzimuthRange
        {
            get
            {
                return azimuthRange;
            }
        }
        
        public double Ratio
        {
            get
            {
                return ratio;
            }
        }
        
        public ApertureConstruction ApertureConstruction
        {
            get
            {
                return apertureConstruction;
            }
        }

        public virtual bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("Ratio"))
            {
                ratio = jObject.Value<double>("Ratio");
            }

            if (jObject.ContainsKey("AzimuthRange"))
            {
                azimuthRange = Core.Query.IJSAMObject<Range<double>>(jObject.Value<JObject>("AzimuthRange"));
            }

            if (jObject.ContainsKey("ApertureConstruction"))
            {
                apertureConstruction = Core.Query.IJSAMObject<ApertureConstruction>(jObject.Value<JObject>("ApertureConstruction"));
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JObject result = new();
            result.Add("_type", Core.Query.FullTypeName(this));

            if (azimuthRange != null)
            {
                result.Add("AzimuthRange", azimuthRange.ToJObject());
            }

            if (double.IsNaN(ratio))
            {
                result.Add("Ratio", ratio);
            }

            if (apertureConstruction != null)
            {
                result.Add("ApertureConstruction", apertureConstruction.ToJObject());
            }

            return result;
        }
    }
}
