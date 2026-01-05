using SAM.Core.Attributes;
using System.ComponentModel;

namespace SAM.Analytical
{
    [AssociatedTypes(typeof(PartitionSimulationResult)), Description("PartitionSimulationResult Parameter")]
    public enum PartitionSimulationResultParameter
    {
        [ParameterProperties("Area", "Area [m2]"), DoubleParameterValue()] Area,
    }
}