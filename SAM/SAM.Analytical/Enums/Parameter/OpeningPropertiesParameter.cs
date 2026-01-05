using SAM.Core.Attributes;
using System.ComponentModel;

namespace SAM.Analytical
{
    [AssociatedTypes(typeof(IOpeningProperties)), Description("OpeningProperties Parameter")]
    public enum OpeningPropertiesParameter
    {
        [ParameterProperties("Function", "Function"), ParameterValue(Core.ParameterType.String)] Function,
        [ParameterProperties("Description", "Description"), ParameterValue(Core.ParameterType.String)] Description,
    }
}