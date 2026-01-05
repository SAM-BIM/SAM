using SAM.Core.Attributes;
using System.ComponentModel;

namespace SAM.Analytical
{
    [AssociatedTypes(typeof(IHostPartition)), Description("HostPartition Parameter")]
    public enum HostPartitionParameter
    {
        [ParameterProperties("Adiabatic", "Adiabatic"), ParameterValue(Core.ParameterType.Boolean)] Adiabatic,
    }
}