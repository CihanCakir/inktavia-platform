using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.DeactivateRefundAllocationPolicy;


// ─── Deactivate ──────────────────────────────────────────────────────────────
public sealed class DeactivateRefundAllocationPolicyBffCommand : AizenCommand<DeactivateRefundAllocationPolicyBffResponse>
{
    public long Id { get; init; }
}
public sealed class DeactivateRefundAllocationPolicyBffResponse { public RefundAllocationPolicyMutateResultDto Result { get; init; } = default!; }
