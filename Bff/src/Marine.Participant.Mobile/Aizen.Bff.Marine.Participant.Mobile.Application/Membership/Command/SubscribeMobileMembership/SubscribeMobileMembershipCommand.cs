using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Membership;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Model.Request;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Membership;

/// <summary>POST /api/v1/mobile/membership/subscription — subscribe the caller to a plan. Free → immediate; paid →
/// an iyzico-gated checkout (the returned payment status is Pending until captured). The body carries only the plan
/// id; the price is resolved server-side. Owner identity from the token.</summary>
public sealed class SubscribeMobileMembershipCommand : AizenCommand<MobileSubscribeResultDto>
{
    public SubscribeMobileMembershipCommand(long planId) => PlanId = planId;
    public long PlanId { get; }
}
