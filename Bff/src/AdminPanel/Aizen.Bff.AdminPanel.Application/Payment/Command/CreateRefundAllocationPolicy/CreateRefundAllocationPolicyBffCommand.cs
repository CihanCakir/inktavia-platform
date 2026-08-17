using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.CreateRefundAllocationPolicy;


// ─── Create ──────────────────────────────────────────────────────────────────
public sealed class CreateRefundAllocationPolicyBffCommand : AizenCommand<CreateRefundAllocationPolicyBffResponse>
{
    public CreateRefundAllocationPolicyBffRequest Body { get; init; } = default!;
}
public sealed class CreateRefundAllocationPolicyBffResponse { public RefundAllocationPolicyMutateResultDto Result { get; init; } = default!; }
