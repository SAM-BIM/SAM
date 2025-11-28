using Newtonsoft.Json.Linq;

namespace SAM.Analytical.Classes
{
    public class ApertureConstructionCase : Case
    {
        private ApertureConstruction apertureConstruction;

        public ApertureConstructionCase(ApertureConstruction apertureConstruction)
            : base()
        {
            this.apertureConstruction = apertureConstruction;
        }

        public ApertureConstructionCase(JObject jObject)
            : base(jObject)
        {

        }

        public ApertureConstruction ApertureConstruction
        {
            get
            {
                return apertureConstruction;
            }

            set
            {
                apertureConstruction = value;
                OnPropertyChanged(nameof(ApertureConstruction));
            }
        }
        
        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);
            if (!result)
            {
                return false;
            }

            if (jObject.ContainsKey("ApertureConstruction"))
            {
                apertureConstruction = Core.Query.IJSAMObject<ApertureConstruction>(jObject.Value<JObject>("ApertureConstruction"));
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

            if (apertureConstruction != null)
            {
                result.Add("ApertureConstruction", apertureConstruction.ToJObject());
            }

            return result;
        }
    }
}
