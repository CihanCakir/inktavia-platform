using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Authorization;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Membership;
using Aizen.Bff.Marine.Participant.Mobile.Application.Membership;
using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.Infrastructure.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Bff.Marine.Participant.Mobile.Controllers.V1;

/// <summary>
/// BE_MO7 — the owner participant-membership surface: active plans, the caller's current subscription, subscribe
/// (free-immediate / paid iyzico-gated), cancel, and the paid-checkout payment status. Identity is resolved
/// server-side from the validated token and asserted to Payment (the profile id is never in the body); the price is
/// resolved from the plan server-side. Cost-free.
/// </summary>
[ApiController]
[Route("api/v1/mobile/membership")]
[Tags("Mobile - Membership")]
[Authorize(Policy = ParticipantAuthorizationPolicies.ParticipantAuthenticated)]
public sealed class MembershipController : AizenWebApiController
{
    private readonly IAizenCQRSProcessor _cqrs;

    public MembershipController(IHttpContextAccessor httpContextAccessor, IAizenCQRSProcessor cqrs)
        : base(httpContextAccessor)
    {
        _cqrs = cqrs;
    }

    /// <summary>The active participant plans to compare/subscribe (name, ₺ price, discount rate, perks). Cost-free.</summary>
    [HttpGet("plans")]
    [ProducesResponseType(typeof(List<MobileMembershipPlanDto>), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<List<MobileMembershipPlanDto>>> GetPlans(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileMembershipPlansQuery(), ct);
        return SetResponse(result);
    }

    /// <summary>The caller's current active subscription; <c>null</c> when they have no plan.</summary>
    [HttpGet("subscription")]
    [ProducesResponseType(typeof(MobileCurrentSubscriptionDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileCurrentSubscriptionDto>> GetSubscription(CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileCurrentSubscriptionQuery(), ct);
        return SetResponse(result);
    }

    /// <summary>Subscribe to a plan. Free/launch → applied immediately; paid → an iyzico-gated checkout (poll the
    /// payment status). The body carries only the plan id.</summary>
    [HttpPost("subscription")]
    [ProducesResponseType(typeof(MobileSubscribeResultDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileSubscribeResultDto>> Subscribe(
        [FromBody] MobileSubscribeRequest request, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(
            new SubscribeMobileMembershipCommand(request?.PlanId ?? 0), ct);
        return SetResponse(result);
    }

    /// <summary>Cancel the caller's active subscription (access retained until period end; no refund).</summary>
    [HttpDelete("subscription")]
    [ProducesResponseType(typeof(MobileCancelSubscriptionDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileCancelSubscriptionDto>> Cancel(
        [FromQuery] string? reason, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new CancelMobileMembershipCommand(reason), ct);
        return SetResponse(result);
    }

    /// <summary>The payment status of a paid subscription's checkout (poll: Pending → Paid/Failed). Cost-free.</summary>
    [HttpGet("subscription/payment-status/{transactionId:long}")]
    [ProducesResponseType(typeof(MobileMembershipPaymentStatusDto), StatusCodes.Status200OK)]
    public async Task<AizenApiResponse<MobileMembershipPaymentStatusDto>> GetPaymentStatus(
        [FromRoute] long transactionId, CancellationToken ct)
    {
        var result = await _cqrs.ProcessAsync(new GetMobileSubscriptionPaymentStatusQuery(transactionId), ct);
        return SetResponse(result);
    }
}
