using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Vessel.Abstraction.Response.Status;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Query;

[DocumentationInfo("Get admin vessel status history query handler", "Fetches vessel status change history from the Vessel module.")]
public sealed class GetVesselStatusHistoryBffQueryHandler
    : AizenQueryHandler<GetVesselStatusHistoryBffQuery, GetVesselStatusHistoryResponse>
{
    private readonly IVesselRemoteCall _vessel;

    public GetVesselStatusHistoryBffQueryHandler(
        IVesselRemoteCall vessel)
    {
        _vessel = vessel;
    }

    public override async Task<GetVesselStatusHistoryResponse?> Handle(
        GetVesselStatusHistoryBffQuery request, CancellationToken cancellationToken)
    {

        var result = await _vessel.GetVesselStatusHistory(
            request.VesselId,
            request.PageIndex,
            request.PageSize);

        return result.Body;
    }
}
