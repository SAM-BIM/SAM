using SAM.Core.Attributes;
using System.ComponentModel;

namespace SAM.Core
{
    [AssociatedTypes(typeof(Material)), Description("Material Parameter")]
    public enum MaterialParameter
    {
        [ParameterProperties("Default Thickness", "Default Material Thickness"), DoubleParameterValue(0)] DefaultThickness,
    }
}