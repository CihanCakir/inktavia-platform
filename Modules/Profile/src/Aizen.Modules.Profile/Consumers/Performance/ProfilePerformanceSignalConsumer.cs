using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Payment.Abstraction.Message;
using Aizen.Modules.Profile.Abstraction.Message.Performance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Profile.Consumers.Performance;

/// <summary>
/// Listens for ServiceRequestCompletedMessage from the Payment module.
/// When a service request is completed, publishes ProfilePerformanceRecomputeRequestedMessage
/// so that the provider's performance score is asynchronously recalculated.
///
/// ── Trigger events handled by this consumer ──────────────────────────────────
///   ServiceRequestCompletedMessage → ProviderProfileId extracted directly.
///
/// ── Trigger events planned (post-MVP, separate consumers) ─────────────────────
///   CargoDryKitActivatedMessage  → ProviderProfileId mapping via CargoDry API (kit.ProviderProfileId)
///   CargoDryKitRenewedMessage    → Same as above
///   PayoutCompletedMessage       → ProviderProfileId available directly
///   PayoutFailedMessage          → ProviderProfileId available directly (post-MVP message)
///
/// ── Provider-only rule ────────────────────────────────────────────────────────
///   Only Provider profiles are scored (Phase 19). This consumer always publishes
///   ProfileType = 1 (Provider). Non-provider triggers MUST be handled in dedicated consumers.
///
/// ── Reliability ───────────────────────────────────────────────────────────────
///   Publish happens in ExecuteCommitMessage (2-phase, after Prepare succeeds).
///   No fire-and-forget. No in-process scheduling.
///
/// ── Idempotency key format ────────────────────────────────────────────────────
///   "SR-{ServiceRequestId}-{ProviderProfileId}"
///   Unique per SR completion event per provider — safe to deliver multiple times.
///
/// ── DI registration ───────────────────────────────────────────────────────────
///   Registered by AizenApplicationBuilder assembly scanning from Aizen.Modules.Profile.
///   Requires: Aizen.Modules.Payment.Abstraction project reference in Profile.csproj.
/// </summary>
public sealed class ProfilePerformanceSignalConsumer
    : AizenBaseMessageConsumer<ServiceRequestCompletedMessage>
{
    private readonly IAizenMessagePublisher                            _publisher;
    private readonly ILogger<ProfilePerformanceSignalConsumer>         _logger;

    public ProfilePerformanceSignalConsumer(IServiceProvider sp) : base(sp)
    {
        _publisher = sp.GetRequiredService<IAizenMessagePublisher>();
        _logger    = sp.GetRequiredService<ILogger<ProfilePerformanceSignalConsumer>>();
    }

    /// <summary>
    /// Prepare phase: validate the provider ID is present and positive.
    /// No DB writes in this phase — the actual recalculation is dispatched via the commit phase.
    /// Returns false if the message should not trigger a recalculation (e.g. no provider).
    /// </summary>
    public override Task<bool> ExecutePrepareMessage(
        ServiceRequestCompletedMessage message, CancellationToken ct)
    {
        if (message.ProviderProfileId <= 0)
        {
            _logger.LogWarning(
                "ProfilePerformanceSignalConsumer: SR {ServiceRequestId} has no valid ProviderProfileId ({Value}). Skipping.",
                message.ServiceRequestId, message.ProviderProfileId);
            return Task.FromResult(false);
        }

        _logger.LogDebug(
            "ProfilePerformanceSignalConsumer: SR {ServiceRequestId} completed for Provider {ProviderId}. Scheduling recompute.",
            message.ServiceRequestId, message.ProviderProfileId);

        return Task.FromResult(true);
    }

    /// <summary>
    /// Commit phase: publish ProfilePerformanceRecomputeRequestedMessage.
    /// Executes only after Prepare returned true — guarantees at-least-once reliable delivery.
    /// </summary>
    public override async Task ExecuteCommitMessage(
        ServiceRequestCompletedMessage message, CancellationToken ct)
    {
        var recomputeMessage = new ProfilePerformanceRecomputeRequestedMessage
        {
            ProfileId      = message.ProviderProfileId,
            ProfileType    = 1,  // Provider
            TriggerReason  = $"ServiceRequestCompleted-SR{message.ServiceRequestId}",
            SourceModule   = "ServiceRequest",
            SourceEntityId = message.ServiceRequestId,
            IdempotencyKey = $"SR-{message.ServiceRequestId}-{message.ProviderProfileId}",
            RequestedAtUtc = DateTime.UtcNow,
        };

        await _publisher.PublishAsync(recomputeMessage, ct);

        _logger.LogInformation(
            "ProfilePerformanceSignalConsumer: published ProfilePerformanceRecomputeRequestedMessage " +
            "for Provider {ProviderId} (SR {ServiceRequestId}).",
            message.ProviderProfileId, message.ServiceRequestId);
    }

    /// <summary>
    /// Rollback phase: log the error. No compensation needed — no DB writes were made in Prepare.
    /// </summary>
    public override Task ExecuteRollbackMessage(
        ServiceRequestCompletedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogError(
            "ProfilePerformanceSignalConsumer: rollback for SR {ServiceRequestId} — Source={ErrorSource}, Error={ErrorMessage}",
            message.ServiceRequestId, ex.ErrorSource, ex.Message);
        return Task.CompletedTask;
    }
}
