using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPartCommercialTermsList;


// ─── PartCommercialTerm: List (no paging) ────────────────────────────────────
public sealed class GetPartCommercialTermsListBffQuery : AizenQuery<GetPartCommercialTermsListBffResponse>
{
    public string? Brand             { get; init; }
    public string? ProductCode       { get; init; }
    public long?   ProviderProfileId { get; init; }
    public string? CategoryCode      { get; init; }
    public string? CurrencyCode      { get; init; }
    public bool?   IsActive          { get; init; }
}
public sealed class GetPartCommercialTermsListBffResponse { public PartCommercialTermListBffResult Result { get; init; } = default!; }
