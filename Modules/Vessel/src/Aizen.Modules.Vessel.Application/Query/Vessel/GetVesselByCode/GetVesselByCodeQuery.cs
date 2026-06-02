using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Modules.Vessel.Application.Query.Vessel;

public sealed class GetVesselByCodeQuery : AizenQuery<GetVesselByCodeResponse>
{
    public string VesselCode { get; }

    public GetVesselByCodeQuery(string vesselCode)
    {
        VesselCode = vesselCode;
    }
}
