﻿using Grasshopper.Kernel.Types;

namespace SAM.Core.Grasshopper
{
    public class GooSAMObject<T> : GH_Goo<T>, IGooSAMObject where T : SAMObject
    {
        public GooSAMObject(T sAMObject)
        {
            Value = sAMObject;
        }

        public override bool IsValid => Value != null;

        public override string TypeName => Value.GetType().FullName;

        public override string TypeDescription => Value.GetType().FullName;

        public override IGH_Goo Duplicate()
        {
            return new GooSAMObject<T>(Value);
        }

        public SAMObject GetSAMObject()
        {
            return Value;
        }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Value.Name))
                return Value.Name;

            return typeof(T).FullName;
        }
    }
}
