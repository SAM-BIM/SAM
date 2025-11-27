using Newtonsoft.Json.Linq;

namespace SAM.Analytical.Classes
{
    public class WindowSizeCase : Case
    {
        private double apertureScaleFactor;

        public WindowSizeCase(double apertureScaleFactor)
            : base()
        {
            this.apertureScaleFactor = apertureScaleFactor;
        }

        public WindowSizeCase(JObject jObject)
            : base(jObject)
        {

        }

        public double ApertureScaleFactor
        {
            get
            {
                return apertureScaleFactor;
            }

            set
            {
                apertureScaleFactor = value;
                OnPropertyChanged(nameof(ApertureScaleFactor));
            }
        }
        
        public override bool FromJObject(JObject jObject)
        {
            bool result = base.FromJObject(jObject);
            if (!result)
            {
                return false;
            }

            if (jObject.ContainsKey("ApertureScaleFactor"))
            {
                apertureScaleFactor = jObject.Value<double>("ApertureScaleFactor");
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

            if (!double.IsNaN(apertureScaleFactor))
            {
                result.Add("ApertureScaleFactor", apertureScaleFactor);
            }

            return result;
        }
    }
}
