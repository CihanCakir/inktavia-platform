using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetRefundAllocationPolicyById;


// ─── Get by id ───────────────────────────────────────────────────────────────
public sealed class GetRefundAllocationPolicyByIdBffQuery : AizenQuery<GetRefundAllocationPolicyByIdBffResponse>
{
    public long Id { get; init; }
}
public sealed class GetRefundAllocationPolicyByIdBffResponse { public RefundAllocationPolicyAdminDto? Result { get; init; } }
