using Newtonsoft.Json.Linq;
using SAM.Core;
using System.Collections;
using System.Collections.Generic;

namespace SAM.Analytical.Classes
{
    public class Cases : IJSAMObject, IAnalyticalObject, IEnumerable<Case>
    {
        private List<Case> values = [];

        public Cases()
        {
            
        }

        public Cases(IEnumerable<Case> cases)
        {
            values = cases == null ? [] : [.. cases];
        }

        public Cases(JObject jObject)
        {
            FromJObject(jObject);
        }

        public int Count
        {
            get
            {
                return values?.Count ?? 0;
            }
        }

        public Case this[int index]
        {
            get
            {
                if (values == null || index < 0 || index >= values.Count)
                {
                    return null;
                }
                return values[index];
            }
            set
            {
                if (values == null || index < 0 || index >= values.Count)
                {
                    return;
                }
                values[index] = value;
            }
        }

        public void Add(Case @case)
        {
            if (@case == null)
            {
                return;
            }

            if (values == null)
            {
                values = [];
            }

            values.Add(@case);
        }

        public virtual bool FromJObject(JObject jObject)
        {
            if (jObject == null)
            {
                return false;
            }

            if (jObject.ContainsKey("Values"))
            {
                JArray jArray = jObject.Value<JArray>("Values");
                if(jArray != null)
                {
                    values = [];
                    foreach (JObject jObject_Temp in jArray)
                    {
                        Case @case = Core.Query.IJSAMObject<Case>(jObject_Temp);
                        if (@case == null)
                        {
                            continue;
                        }

                        values.Add(@case);
                    }
                }
            }

            return true;
        }

        public IEnumerator<Case> GetEnumerator()
        {
            return values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public virtual JObject ToJObject()
        {
            JObject result = new JObject();
            result.Add("_type", Core.Query.FullTypeName(this));

            if (values != null)
            {
                JArray jArray = [];
                foreach(Case value in values)
                {
                    if(value?.ToJObject() is JObject jObject_Temp)
                    {
                        jArray.Add(jObject_Temp);
                    }
                }

                result.Add("Values", jArray);
            }

            return result;
        }
    }
}
