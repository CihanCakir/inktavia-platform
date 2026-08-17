using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetProfitProtectionPolicyDetail;


// ─── Detail (by id) ──────────────────────────────────────────────────────────
public sealed class GetProfitProtectionPolicyDetailBffQuery : AizenQuery<GetProfitProtectionPolicyDetailBffResponse>
{
    public long Id { get; init; }
}
public sealed class GetProfitProtectionPolicyDetailBffResponse { public ProfitProtectionPolicyDetailBffDto? Policy { get; init; } }
