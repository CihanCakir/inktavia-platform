using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Model.Request;
using Aizen.Modules.Payment.Abstraction.Model.Result;
using Aizen.Modules.Payment.Application.Commands.CancelParticipantSubscription;
using Aizen.Modules.Payment.Application.Commands.SubscribeParticipantForOwner;
using Aizen.Modules.Payment.Application.Queries.GetActiveParticipantSubscription;
using Aizen.Modules.Payment.Application.Queries.GetParticipantPlans;
using Aizen.Modules.Payment.Application.Queries.GetParticipantSubscriptionPaymentStatusForOwner;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

/// <summary>
/// BE-MO7 — the owner-facing participant membership surface: active plans, the caller's current subscription,
/// subscribe (free-immediate / paid iyzico-gated), cancel, and the paid-checkout payment status. The participant
/// identity is resolved from the trusted context (the mobile BFF asserts the participant profile id via
/// X-Aizen-Provider-Profile-Id → <c>KeycloakTokenInfo.ProviderProfileId</c>), never from the body. Mirrors
/// <c>PaymentProviderController</c>'s token-identity pattern. Reads/subscribe/cancel reuse the existing Payment
/// queries/commands; no plan pricing / discount / billing logic changes here.
/// </summary>
[ApiController]
[Route("api/v1/payment/participant")]
[Tags("Payment - Participant Membership")]
[Authorize]
public sealed class ParticipantMembershipController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;
    private readonly IAizenInfoAccessor _info;

    public ParticipantMembershipController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs, IAizenInfoAccessor info)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
        _info = info;
    }

    private long ResolveParticipantProfileId()
    {
        // The mobile BFF asserts the resolved participant profile id in the shared asserted-profile-id header,
        // which the middleware surfaces on KeycloakTokenInfo.ProviderProfileId (the generic "asserted profile id").
        var pid = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (pid <= 0) throw new AizenBusinessException("Participant identity could not be resolved.");
        return pid;
    }

    /// <summary>The active participant plans available to subscribe to (name, price, discount rate, perks).</summary>
    [HttpGet("plans")]
    [ProducesResponseType(typeof(List<ParticipantPlanDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<ParticipantPlanDto>?>> GetPlans(CancellationToken ct = default)
    {
        var result = await _cqrs.ProcessAsync<List<ParticipantPlanDto>>(
            new GetParticipantPlansQuery { IncludeInactive = false }, ct);
        return SetResponse(result);
    }

    /// <summary>The caller's currently active subscription (null when they have no plan).</summary>
    [HttpGet("subscription")]
    [ProducesResponseType(typeof(ActiveParticipantSubscriptionResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ActiveParticipantSubscriptionResult?>> GetSubscription(CancellationToken ct = default)
    {
        var pid = ResolveParticipantProfileId();
        var result = await _cqrs.ProcessAsync<ActiveParticipantSubscriptionResult>(
            new GetActiveParticipantSubscriptionQuery { ParticipantProfileId = pid }, ct);
        return SetResponse(result);
    }

    /// <summary>Subscribe the caller to a plan. Free/launch → applied immediately; paid → an iyzico-gated checkout
    /// (poll the payment status). The price is resolved from the plan server-side; the body carries only the plan id.</summary>
    [HttpPost("subscription")]
    [ProducesResponseType(typeof(SubscribeParticipantForOwnerResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<SubscribeParticipantForOwnerResult?>> Subscribe(
        [FromBody] SubscribeParticipantForOwnerRequest request, CancellationToken ct = default)
    {
        var pid = ResolveParticipantProfileId();
        var result = await _cqrs.ProcessAsync<SubscribeParticipantForOwnerResult>(
            new SubscribeParticipantForOwnerCommand { ParticipantProfileId = pid, ParticipantPlanId = request.PlanId }, ct);
        return SetResponse(result);
    }

    /// <summary>Cancel the caller's active subscription (access retained until period end; no refund).</summary>
    [HttpDelete("subscription")]
    [ProducesResponseType(typeof(CancelSubscriptionResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<CancelSubscriptionResult?>> Cancel(
        [FromQuery] string? reason, CancellationToken ct = default)
    {
        var pid = ResolveParticipantProfileId();
        var result = await _cqrs.ProcessAsync<CancelSubscriptionResult>(
            new CancelParticipantSubscriptionCommand { ParticipantProfileId = pid, CancellationReason = reason }, ct);
        return SetResponse(result);
    }

    /// <summary>The payment status of a paid subscription's checkout (poll: Pending → Paid/Failed). Owner-scoped —
    /// the transaction must be this participant's subscription checkout.</summary>
    [HttpGet("subscription/payment-status/{transactionId:long}")]
    [ProducesResponseType(typeof(ParticipantSubscriptionPaymentStatusResult), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<ParticipantSubscriptionPaymentStatusResult?>> GetPaymentStatus(
        [FromRoute] long transactionId, CancellationToken ct = default)
    {
        var pid = ResolveParticipantProfileId();
        var result = await _cqrs.ProcessAsync<ParticipantSubscriptionPaymentStatusResult>(
            new GetParticipantSubscriptionPaymentStatusForOwnerQuery { ParticipantProfileId = pid, TransactionId = transactionId }, ct);
        return SetResponse(result);
    }
}
