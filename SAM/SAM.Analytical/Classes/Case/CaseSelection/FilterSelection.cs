using Newtonsoft.Json.Linq;

namespace SAM.Analytical.Classes
{
    public abstract class FilterSelection : CaseSelection
    {
        private Filter filter;

        public FilterSelection(Filter filter)
        {
            this.filter = filter;
        }

        public FilterSelection()
        {
            
        }

        public FilterSelection(JObject jObject)
        {
            FromJObject(jObject);
        }

        public Filter Filter
        {
            get 
            { 
                return filter; 
            }
            
            set 
            { 
                filter = value; 
            }
        }

        public virtual bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("Filter"))
            {
                filter = Core.Query.IJSAMObject<Filter>(jObject.Value<JObject>("Filter"));
            }

            return true;
        }

        public virtual JObject ToJObject()
        {
            JObject result = new JObject();
            result.Add("_type", Core.Query.FullTypeName(this));

            if (filter != null)
            {
                result.Add("Filter", filter.ToJObject());
            }

            return result;
        }
    }
}
