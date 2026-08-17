using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>GET /api/v1/mobile/service-requests/disputes — the caller-owner's own disputes (on the SRs they own) +
/// a global open/actionable count. Owner scoped module-side via the asserted UserInfo.UserId (never a parameter).
/// Cost-free. Optionally narrowed to one status name.</summary>
public sealed class GetMobileOwnerDisputesQuery : AizenQuery<MobileDisputeListDto>
{
    public GetMobileOwnerDisputesQuery(int pageIndex, int pageSize, string? status)
    {
        PageIndex = pageIndex < 0 ? 0 : pageIndex;
        PageSize = pageSize is <= 0 or > 100 ? 20 : pageSize;
        Status = status;
    }

    public int PageIndex { get; }
    public int PageSize { get; }
    /// <summary>Optional ServiceRequestDisputeStatus name; unrecognized/blank → all statuses.</summary>
    public string? Status { get; }
}
