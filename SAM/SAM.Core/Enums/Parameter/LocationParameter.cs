using SAM.Core.Attributes;
using System.ComponentModel;

namespace SAM.Core
{
    [AssociatedTypes(typeof(Location)), Description("Location Parameter")]
    public enum LocationParameter
    {
        [ParameterProperties("Time Zone", "UTC TimeZone"), ParameterValue(ParameterType.String)] TimeZone,
    }
}