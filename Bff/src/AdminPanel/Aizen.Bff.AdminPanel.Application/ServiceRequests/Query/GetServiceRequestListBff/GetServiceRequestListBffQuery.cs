using Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

public sealed class GetServiceRequestListBffQuery : AizenQuery<AdminServiceRequestListResponse>
{
    public string? Status      { get; }
    public long?   VesselId    { get; }
    public long?   OwnerUserId { get; }
    public int     PageIndex   { get; }
    public int     PageSize    { get; }
    public GetServiceRequestListBffQuery(string? status, long? vesselId, long? ownerUserId, int pageIndex, int pageSize)
    {
        Status      = status;
        VesselId    = vesselId;
        OwnerUserId = ownerUserId;
        PageIndex   = pageIndex;
        PageSize    = pageSize;
    }
}
