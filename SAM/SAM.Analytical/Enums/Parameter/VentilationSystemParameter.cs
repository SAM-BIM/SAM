using SAM.Core.Attributes;
using System.ComponentModel;

namespace SAM.Analytical
{
    [AssociatedTypes(typeof(VentilationSystem)), Description("Ventilation System Parameter")]
    public enum VentilationSystemParameter
    {
        [ParameterProperties("Supply Unit Name", "Supply Unit Name"), ParameterValue(Core.ParameterType.String)] SupplyUnitName,
        [ParameterProperties("Exhaust Unit Name", "Exhaust Unit Name"), ParameterValue(Core.ParameterType.String)] ExhaustUnitName,
    }
}