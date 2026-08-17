using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.ResolveRefundAllocationPolicy;


// ─── Resolve (point-in-time) ─────────────────────────────────────────────────
public sealed class ResolveRefundAllocationPolicyBffQuery : AizenQuery<ResolveRefundAllocationPolicyBffResponse>
{
    public string    CurrencyCode { get; init; } = "TRY";
    public DateTime? AtUtc        { get; init; }
}
public sealed class ResolveRefundAllocationPolicyBffResponse { public RefundAllocationPolicyAdminDto? Result { get; init; } }
