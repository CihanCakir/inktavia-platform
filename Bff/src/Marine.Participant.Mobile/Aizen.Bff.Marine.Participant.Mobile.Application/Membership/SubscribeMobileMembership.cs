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

public sealed class SubscribeMobileMembershipCommandHandler
    : AizenCommandHandler<SubscribeMobileMembershipCommand, MobileSubscribeResultDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantMembershipRemoteCall _membership;

    public SubscribeMobileMembershipCommandHandler(
        IParticipantProfileResolver resolver, IParticipantMembershipRemoteCall membership)
    {
        _resolver = resolver;
        _membership = membership;
    }

    public override async Task<MobileSubscribeResultDto?> Handle(
        SubscribeMobileMembershipCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        if (request.PlanId <= 0)
            throw new AizenBusinessException("A plan must be selected to subscribe.");

        // Reuse the Payment subscribe (free-immediate / paid-checkout). Price + participant are server-resolved.
        var resp = await _membership.Subscribe(new SubscribeParticipantForOwnerRequest { PlanId = request.PlanId });
        var result = resp?.Body
            ?? throw new AizenBusinessException("Could not start the subscription.");

        // Derive the payment lifecycle the FE drives the pay flow on: a paid plan reads the freshly-created checkout
        // status (Pending on the manual gateway until captured); a free plan needs none.
        MobileMembershipPaymentStatusDto payment;
        if (string.Equals(result.Mode, "PaymentPending", StringComparison.OrdinalIgnoreCase)
            && result.TransactionId is { } txId)
        {
            var statusResp = await _membership.GetPaymentStatus(txId);
            payment = MobileMembershipMapper.MapPaymentStatus(statusResp?.Body);
        }
        else
        {
            payment = new MobileMembershipPaymentStatusDto { HasPayment = false, Status = "None" };
        }

        return MobileMembershipMapper.MapSubscribeResult(result, payment);
    }
}
