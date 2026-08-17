using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Command.UpdateRefundAllocationPolicy;


// ─── Update ──────────────────────────────────────────────────────────────────
public sealed class UpdateRefundAllocationPolicyBffCommand : AizenCommand<UpdateRefundAllocationPolicyBffResponse>
{
    public long                                   Id   { get; init; }
    public UpdateRefundAllocationPolicyBffRequest Body { get; init; } = default!;
}
public sealed class UpdateRefundAllocationPolicyBffResponse { public RefundAllocationPolicyMutateResultDto Result { get; init; } = default!; }
