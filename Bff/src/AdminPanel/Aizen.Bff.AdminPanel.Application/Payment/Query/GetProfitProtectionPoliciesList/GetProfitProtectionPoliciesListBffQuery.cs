using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProfitProtectionPoliciesList;


// ─── List (version history — no paging) ──────────────────────────────────────
public sealed class GetProfitProtectionPoliciesListBffQuery : AizenQuery<GetProfitProtectionPoliciesListBffResponse>
{
    public string? CurrencyCode { get; init; }
    public string? Status       { get; init; }
    public bool?   IsActive     { get; init; }
}
public sealed class GetProfitProtectionPoliciesListBffResponse { public ProfitProtectionPolicyListBffResult Result { get; init; } = default!; }
