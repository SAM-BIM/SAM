using SAM.Analytical.Classes;
using SAM.Core;
using System.Collections.Generic;

namespace SAM.Analytical
{
    public static partial class Query
    {
        public static List<TJSAMObject> IJSAMObjects<TJSAMObject>(this CaseSelection caseSelection, AnalyticalModel analyticalModel) where TJSAMObject : IJSAMObject
        {
            if(caseSelection is null || analyticalModel is null)
            {
                return null;
            }


            if(caseSelection is ObjectReferenceCaseSelection objectReferenceCaseSelection)
            {
                return IJSAMObjects<TJSAMObject>(objectReferenceCaseSelection, analyticalModel);
            }

            throw new System.NotImplementedException();

        }

        public static List<TJSAMObject> IJSAMObjects<TJSAMObject>(this ObjectReferenceCaseSelection objectReferenceCaseSelection, AnalyticalModel analyticalModel) where TJSAMObject : IJSAMObject
        {
            if(objectReferenceCaseSelection is null || analyticalModel is null)
            {
                return null;
            }

            List<TJSAMObject> result = [];

            if(objectReferenceCaseSelection.ObjectReferences is not IEnumerable<ObjectReference> objectReferences)
            {
                return result;
            }

            foreach (ObjectReference objectReference in objectReferences)
            {
                TJSAMObject jSAMObject = analyticalModel.GetObject<TJSAMObject>(objectReference);
                if(jSAMObject is null)
                {
                    continue;
                }

                result.Add(jSAMObject);
            }

            return result;
        }
    }
}