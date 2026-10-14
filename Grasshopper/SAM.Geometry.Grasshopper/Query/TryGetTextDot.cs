// SPDX-License-Identifier: LGPL-3.0-or-later
// Copyright (c) 2020–2026 Michal Dengusiak & Jakub Ziolkowski and contributors

using Grasshopper.Kernel.Types;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace SAM.Geometry.Grasshopper
{
    public static partial class Query
    {
        /// <summary>
        /// Resolves a Rhino Text Dot from a Grasshopper value: a Rhino <see cref="TextDot"/>, a
        /// <see cref="GH_TextDot"/>, a <see cref="RhinoObject"/> or <see cref="ObjRef"/>, or any Grasshopper goo that
        /// can cast to a Text Dot - including the Rhino 8 "Model Object" produced by Query Model Objects.
        /// </summary>
        /// <param name="object">The value to resolve.</param>
        /// <param name="textDot">When this method returns, the resolved Rhino Text Dot, or null.</param>
        /// <returns>True if a Text Dot was resolved; otherwise false.</returns>
        public static bool TryGetTextDot(object @object, out TextDot textDot)
        {
            textDot = null;

            if (@object == null)
            {
                return false;
            }

            if (@object is TextDot textDot_Temp)
            {
                textDot = textDot_Temp;
                return textDot != null;
            }

            if (@object is RhinoObject rhinoObject)
            {
                return TryGetTextDot(rhinoObject.Geometry, out textDot);
            }

            if (@object is ObjRef objRef)
            {
                RhinoObject rhinoObject_Temp = objRef.Object();
                return TryGetTextDot(rhinoObject_Temp == null ? null : rhinoObject_Temp.Geometry, out textDot);
            }

            if (@object is IGH_Goo goo)
            {
                try
                {
                    if (goo.CastTo(out TextDot textDot_FromGoo) && textDot_FromGoo != null)
                    {
                        textDot = textDot_FromGoo;
                        return true;
                    }

                    if (goo.CastTo(out GH_TextDot gH_TextDot_FromGoo) && gH_TextDot_FromGoo != null && gH_TextDot_FromGoo.Value != null)
                    {
                        textDot = gH_TextDot_FromGoo.Value;
                        return true;
                    }
                }
                catch
                {
                }
            }

            return false;
        }
    }
}
