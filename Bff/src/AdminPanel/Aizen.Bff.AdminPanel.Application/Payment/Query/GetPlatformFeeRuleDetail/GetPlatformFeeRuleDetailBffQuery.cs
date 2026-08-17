using Aizen.Bff.AdminPanel.Application.Payment.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.Payment.Query.GetPlatformFeeRuleDetail;


// ─── Detail (by id) ──────────────────────────────────────────────────────────
public sealed class GetPlatformFeeRuleDetailBffQuery : AizenQuery<GetPlatformFeeRuleDetailBffResponse>
{
    public long Id { get; init; }
}
public sealed class GetPlatformFeeRuleDetailBffResponse { public PlatformFeeRuleDetailBffDto? Rule { get; init; } }
