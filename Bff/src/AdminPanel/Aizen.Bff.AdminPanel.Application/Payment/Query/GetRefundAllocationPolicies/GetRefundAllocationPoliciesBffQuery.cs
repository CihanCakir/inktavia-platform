using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Payment.Abstraction.Dto;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetRefundAllocationPolicies;


// ─── List ────────────────────────────────────────────────────────────────────
public sealed class GetRefundAllocationPoliciesBffQuery : AizenQuery<GetRefundAllocationPoliciesBffResponse> { }
public sealed class GetRefundAllocationPoliciesBffResponse { public List<RefundAllocationPolicyAdminDto> Items { get; init; } = new(); }
