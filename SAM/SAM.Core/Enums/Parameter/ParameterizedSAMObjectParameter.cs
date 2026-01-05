using SAM.Core.Attributes;
using System.ComponentModel;

namespace SAM.Core
{
    [AssociatedTypes(typeof(IParameterizedSAMObject)), Description("ParameterizedSAMObject Parameter")]
    public enum ParameterizedSAMObjectParameter
    {
        [ParameterProperties("Category", "Category"), SAMObjectParameterValue(typeof(Category))] Category,
    }
}