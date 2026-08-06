using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPartCommercialTermDetail;


// ─── PartCommercialTerm: Detail (by id) ──────────────────────────────────────
public sealed class GetPartCommercialTermDetailBffQuery : AizenQuery<GetPartCommercialTermDetailBffResponse>
{
    public long Id { get; init; }
}
public sealed class GetPartCommercialTermDetailBffResponse { public PartCommercialTermBffDto? Term { get; init; } }
