using Aizen.Modules.Payment.Abstraction.Model.Request;
using Aizen.Modules.Payment.Application.Commands.CancelParticipantSubscription;
using Aizen.Modules.Payment.Application.Commands.CancelProviderSubscription;
using Aizen.Modules.Payment.Application.Commands.SubscribeParticipantPlan;
using Aizen.Modules.Payment.Application.Commands.SubscribeProviderPlan;
using Aizen.Modules.Payment.Application.Queries.GetActiveParticipantSubscription;
using Aizen.Modules.Payment.Application.Queries.GetActiveProviderSubscription;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.Payment.Controllers;

/// <summary>
/// Manages provider and participant plan subscriptions.
///
/// All mutations (subscribe, cancel) require Admin or internal-service authorization.
/// Queries (status) can be called by admin or internal services; adapt auth as needed for MVP.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/v1/payment/subscriptions")]
public sealed class SubscriptionController : ControllerBase
{
    private readonly ISender _sender;
    public SubscriptionController(ISender sender) => _sender = sender;

    // ── Provider Subscriptions ─────────────────────────────────────────────────

    /// <summary>Subscribe a provider to a plan for one billing period.</summary>
    [HttpPost("provider")]
    public async Task<IActionResult> SubscribeProvider(
        [FromBody] SubscribeProviderPlanRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new SubscribeProviderPlanCommand
        {
            ProviderProfileId    = request.ProviderProfileId,
            ProviderPlanId       = request.ProviderPlanId,
            PaidAmount           = request.PaidAmount,
            CurrencyCode         = request.CurrencyCode,
            PeriodStart          = request.PeriodStart,
            PeriodEnd            = request.PeriodEnd,
            AutoRenew            = request.AutoRenew,
            PaymentTransactionId = request.PaymentTransactionId,
        }, ct);

        return Ok(result);
    }

    /// <summary>Cancel the active provider subscription.</summary>
    [HttpDelete("provider/{providerProfileId:long}")]
    public async Task<IActionResult> CancelProviderSubscription(
        long providerProfileId,
        [FromQuery] string? reason,
        CancellationToken ct)
    {
        var result = await _sender.Send(new CancelProviderSubscriptionCommand
        {
            ProviderProfileId   = providerProfileId,
            CancellationReason  = reason,
        }, ct);

        return Ok(result);
    }

    /// <summary>Get the currently active provider subscription (null = no active subscription).</summary>
    [HttpGet("provider/{providerProfileId:long}")]
    public async Task<IActionResult> GetProviderSubscription(long providerProfileId, CancellationToken ct)
    {
        var result = await _sender.Send(
            new GetActiveProviderSubscriptionQuery { ProviderProfileId = providerProfileId }, ct);

        return result is null ? NoContent() : Ok(result);
    }

    // ── Participant Subscriptions ──────────────────────────────────────────────

    /// <summary>Subscribe a participant to a plan for one billing period.</summary>
    [HttpPost("participant")]
    public async Task<IActionResult> SubscribeParticipant(
        [FromBody] SubscribeParticipantPlanRequest request, CancellationToken ct)
    {
        var result = await _sender.Send(new SubscribeParticipantPlanCommand
        {
            ParticipantProfileId = request.ParticipantProfileId,
            ParticipantPlanId    = request.ParticipantPlanId,
            PaidAmount           = request.PaidAmount,
            CurrencyCode         = request.CurrencyCode,
            PeriodStart          = request.PeriodStart,
            PeriodEnd            = request.PeriodEnd,
            AutoRenew            = request.AutoRenew,
            PaymentTransactionId = request.PaymentTransactionId,
        }, ct);

        return Ok(result);
    }

    /// <summary>Cancel the active participant subscription.</summary>
    [HttpDelete("participant/{participantProfileId:long}")]
    public async Task<IActionResult> CancelParticipantSubscription(
        long participantProfileId,
        [FromQuery] string? reason,
        CancellationToken ct)
    {
        var result = await _sender.Send(new CancelParticipantSubscriptionCommand
        {
            ParticipantProfileId = participantProfileId,
            CancellationReason   = reason,
        }, ct);

        return Ok(result);
    }

    /// <summary>Get the currently active participant subscription (null = no active subscription).</summary>
    [HttpGet("participant/{participantProfileId:long}")]
    public async Task<IActionResult> GetParticipantSubscription(long participantProfileId, CancellationToken ct)
    {
        var result = await _sender.Send(
            new GetActiveParticipantSubscriptionQuery { ParticipantProfileId = participantProfileId }, ct);

        return result is null ? NoContent() : Ok(result);
    }
}
