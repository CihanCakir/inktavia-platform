using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

public sealed class GetVesselByCodeQuery : AizenQuery<VesselDto?>
{
    public string VesselCode { get; }

    public GetVesselByCodeQuery(string vesselCode)
    {
        VesselCode = vesselCode;
    }
}
