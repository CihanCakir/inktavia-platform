using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.ResolveProfitProtectionPolicy;


// ─── Resolve (active policy preview) ─────────────────────────────────────────
public sealed class ResolveProfitProtectionPolicyBffQuery : AizenQuery<ResolveProfitProtectionPolicyBffResponse>
{
    public string    CurrencyCode { get; init; } = "TRY";
    public DateTime? AtUtc        { get; init; }
}
public sealed class ResolveProfitProtectionPolicyBffResponse { public ProfitProtectionPolicyResolveBffResult? Result { get; init; } }
