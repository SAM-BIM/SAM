using SAM.Core;

namespace SAM.Analytical
{
    public interface ISelectiveCase : IJSAMObject
    {
        CaseSelection CaseSelection { get; set; }
    }
}
