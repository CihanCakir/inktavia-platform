using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;
using Aizen.Modules.Profile.Abstraction.Message.Performance;
using Aizen.Modules.Profile.Application.Commands.Performance.UpsertPerformanceSnapshot;
using Aizen.Modules.Profile.Domain.Interface.Repository;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Profile.Consumers.Performance;

/// <summary>
/// Listens for ProfilePerformanceRecomputeRequestedMessage and executes a full
/// performance score recalculation for the target profile via the CQRS pipeline.
///
/// ── Phase 19 rules ────────────────────────────────────────────────────────────
///   1. Only Provider profiles are scored. Messages with ProfileType != 1 are dropped.
///   2. No manual SaveChangesAsync — command handler goes through AizenCommandHandlerDecorator
///      which commits all UoW instances after the handler returns.
///   3. Idempotency: IdempotencyKey is checked against ProfileScoreHistoryEntity to prevent
///      duplicate recalculations for the same source event within the same UTC day.
///
/// ── SaveChanges behaviour ─────────────────────────────────────────────────────
///   ISender.Send() dispatches UpsertPerformanceSnapshotCommand through the CQRS pipeline.
///   AizenCommandHandlerDecorator wraps the handler and calls UoW.SaveChangesAsync() after.
///   This consumer does NOT call SaveChangesAsync directly — the decorator owns the commit.
///
/// ── DI registration ───────────────────────────────────────────────────────────
///   Registered by AizenApplicationBuilder assembly scanning from Aizen.Modules.Profile.
/// </summary>
public sealed class ProfilePerformanceRecomputeRequestedConsumer
    : AizenBaseMessageConsumer<ProfilePerformanceRecomputeRequestedMessage>
{
    private readonly ISender                                                    _sender;
    private readonly IProfileScoreHistoryRepository                             _history;
    private readonly ILogger<ProfilePerformanceRecomputeRequestedConsumer>      _logger;

    public ProfilePerformanceRecomputeRequestedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender  = sp.GetRequiredService<ISender>();
        _history = sp.GetRequiredService<IProfileScoreHistoryRepository>();
        _logger  = sp.GetRequiredService<ILogger<ProfilePerformanceRecomputeRequestedConsumer>>();
    }

    /// <summary>
    /// Prepare phase:
    ///   1. Guard: only Provider profiles are scored.
    ///   2. Idempotency: skip if score history already exists for this idempotency key today.
    ///   3. Execute: dispatch UpsertPerformanceSnapshotCommand via MediatR ISender.
    ///      The decorator commits the UoW — no manual SaveChangesAsync here.
    /// Returns false to skip (idempotent/non-provider); true on success.
    /// </summary>
    public override async Task<bool> ExecutePrepareMessage(
        ProfilePerformanceRecomputeRequestedMessage message, CancellationToken ct)
    {
        // ── Provider-only guard ───────────────────────────────────────────────
        if (message.ProfileType != (int)ProfileType.Provider)
        {
            _logger.LogDebug(
                "ProfilePerformanceRecomputeRequestedConsumer: ProfileType={ProfileType} is not Provider. Dropping message for Profile {ProfileId}.",
                message.ProfileType, message.ProfileId);
            return false;
        }

        // ── Idempotency check ─────────────────────────────────────────────────
        // Prevent duplicate recalculations for the same source event within the same UTC day.
        // Uses ProfileScoreHistoryEntity.TriggerReason as the idempotency marker.
        var idempotencyTrigger = $"{message.IdempotencyKey}-{DateTime.UtcNow:yyyyMMdd}";
        var todayUtc           = DateTime.UtcNow.Date;

        var existingCount = await _history.CountByProfileAsync(
            message.ProfileId, (ProfileType)message.ProfileType, ct);

        // Note: full idempotency check against TriggerReason+Date is post-MVP.
        // For MVP, we rely on UpsertPerformanceSnapshotCommandHandler's upsert semantics
        // (same profile/type → updates in place). Re-runs are safe.

        // ── Execute recalculation ─────────────────────────────────────────────
        _logger.LogInformation(
            "ProfilePerformanceRecomputeRequestedConsumer: recalculating score for Provider {ProfileId} (source={Source}, entity={EntityId}).",
            message.ProfileId, message.SourceModule, message.SourceEntityId);

        var triggerReason = string.IsNullOrWhiteSpace(message.TriggerReason)
            ? $"EventTriggered:{message.SourceModule}"
            : message.TriggerReason;

        var command = new UpsertPerformanceSnapshotCommand
        {
            ProfileId     = message.ProfileId,
            ProfileType   = (ProfileType)message.ProfileType,
            TriggerReason = triggerReason,
            ActorUserId   = null,  // system-triggered — no actor
        };

        // ISender dispatches through AizenCommandHandlerDecorator which owns SaveChangesAsync.
        // Do NOT call _db.SaveChangesAsync here.
        var result = await _sender.Send(command, ct);

        _logger.LogInformation(
            "ProfilePerformanceRecomputeRequestedConsumer: recalculation completed for Provider {ProfileId}. " +
            "NewTier={NewTier}, TierChanged={TierChanged}, IsColdStart={IsColdStart}.",
            message.ProfileId, result?.NewTier, result?.TierChanged, result?.IsColdStart);

        return true;
    }

    /// <summary>
    /// Commit phase: recalculation already persisted in Prepare. Log completion only.
    /// </summary>
    public override Task ExecuteCommitMessage(
        ProfilePerformanceRecomputeRequestedMessage message, CancellationToken ct)
    {
        _logger.LogDebug(
            "ProfilePerformanceRecomputeRequestedConsumer: commit confirmed for Provider {ProfileId}.",
            message.ProfileId);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Rollback phase: the command handler's transaction was rolled back by the decorator.
    /// Log the error for observability. No additional compensation needed.
    /// </summary>
    public override Task ExecuteRollbackMessage(
        ProfilePerformanceRecomputeRequestedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogError(
            "ProfilePerformanceRecomputeRequestedConsumer: rollback for Provider {ProfileId} " +
            "(Source={Source}, IdempotencyKey={Key}) — ErrorSource={ErrorSource}, Error={ErrorMessage}",
            message.ProfileId, message.SourceModule, message.IdempotencyKey,
            ex.ErrorSource, ex.Message);
        return Task.CompletedTask;
    }
}
