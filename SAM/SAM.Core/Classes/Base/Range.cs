﻿using Newtonsoft.Json.Linq;

using System.Collections.Generic;
using System.Linq;

namespace SAM.Core
{
    public class Range<T> : IJSAMObject
    {
        private T min;
        private T max;

        public Range(T value_1, T value_2)
        {
            min = System.Math.Min(value_1 as dynamic, value_2 as dynamic);
            max = System.Math.Max(value_1 as dynamic, value_2 as dynamic);
        }

        public Range(T value)
        {
            min = value;
            max = value;
        }

        public Range(IEnumerable<T> values)
        {
            min = default;
            max = default;

            if(values != null)
            {
                List<T> list = values.ToList();
                max = list.Max();
                min = list.Min();
            }
        }

        public Range(Range<T> range)
        {
            min = range.min;
            max = range.max;
        }

        public Range(JObject jObject)
        {
            FromJObject(jObject);
        }

        public T Max
        {
            get
            {
                return max;
            }
        }

        public T Min
        {
            get
            {
                return min;
            }
        }

        public bool Add(T value)
        {
            bool result = false;
            
            if((value as dynamic) > (max as dynamic))
            {
                max = value;
                result = true;
            }

            if ((value as dynamic) < (min as dynamic))
            {
                min = value;
                result = true;
            }

            return result;
        }

        public bool Add(Range<T> value)
        {
            bool result = false;
            if(value == null)
            {
                return false;
            }

            if ((value as dynamic) > (value.max as dynamic))
            {
                max = value.max;
                result = true;
            }

            if ((value as dynamic) < (value.min as dynamic))
            {
                min = value.min;
                result = true;
            }

            return result;
        }

        public bool FromJObject(JObject jObject)
        {
            if (jObject == null)
                return false;

            max = jObject.Value<T>("Max");
            min = jObject.Value<T>("Min");

            return true;
        }

        public JObject ToJObject()
        {
            JObject jObject = new JObject();
            jObject.Add("_type", Query.FullTypeName(this));
            jObject.Add("Max", max as dynamic);
            jObject.Add("Min", min as dynamic);

            return jObject;
        }

        public override string ToString()
        {
            return string.Format("[{0}, {1}]", min, max);
        }

        public bool In(T value)
        {
            return (value as dynamic) <= (max as dynamic) && (value as dynamic) >= (min as dynamic);
        }

        public bool Out(T value)
        {
            return !In(value);
        }

        public bool In(Range<T> range)
        {
            if(range == null)
            {
                return false;
            }

            return (range.min as dynamic) <= (min as dynamic) && (range.max as dynamic) >= (max as dynamic);
        }

        public bool Out(Range<T> range)
        {
            if (range == null)
            {
                return false;
            }

            return (range.min as dynamic) >= (max as dynamic) || (range.max as dynamic) <= (min as dynamic);
        }

        public bool Intersect(Range<T> range)
        {
            return !Out(range);
        }

        public override bool Equals(object @object)
        {
            if (ReferenceEquals(this, null))
                return ReferenceEquals(@object, null) ? true : false;

            return @object is Range<T> range && range.max.Equals(max) && range.min.Equals(min);
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 7) + min.GetHashCode();
            hash = (hash * 7) + max.GetHashCode();
            return hash;
        }

        public static bool operator ==(Range<T> range, object @object)
        {
            if (ReferenceEquals(range, null))
                return ReferenceEquals(@object, null) ? true : false;

            return range.Equals(@object);
        }

        public static bool operator !=(Range<T> range, object @object)
        {
            return !(range == @object);
        }
    }
}