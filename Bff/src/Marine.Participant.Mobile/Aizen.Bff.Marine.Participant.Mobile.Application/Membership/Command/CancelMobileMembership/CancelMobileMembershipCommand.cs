using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Membership;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Membership;

/// <summary>DELETE /api/v1/mobile/membership/subscription — cancel the caller's active subscription (access kept until
/// period end; no refund). Owner identity from the token.</summary>
public sealed class CancelMobileMembershipCommand : AizenCommand<MobileCancelSubscriptionDto>
{
    public CancelMobileMembershipCommand(string? reason) => Reason = reason;
    public string? Reason { get; }
}
