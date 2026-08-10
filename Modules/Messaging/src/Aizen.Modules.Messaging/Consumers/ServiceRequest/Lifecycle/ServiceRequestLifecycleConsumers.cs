using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Messaging.Consumers.ServiceRequest.Lifecycle;

// BE_WC1 — the Messaging module's System/lifecycle message generators. Each consumes a first-class SR domain event and
// writes ONE lifecycle message into the canonical Messaging store via the shared writer (idempotent on the WC0
// sys:{srId}:{CODE} unique index). Zero logic beyond mapping the event → (code, role, type, content). Auto-discovered
// by the messagebus scan (non-generic consumers in the host assembly), exactly like ServiceRequestMessageSyncConsumer.
// They run in PARALLEL with the SR→Messaging sync path until the Messaging:WriteCutover:SystemMessages flag flips the
// SR side off; the shared sys: SourceKey guarantees the two paths collapse to a single row throughout.

/// <summary>OFFER_ACCEPTED System pill from <see cref="ServiceRequestOfferAcceptedMessage"/>.</summary>
public sealed class ServiceRequestOfferAcceptedSystemMessageConsumer
    : AizenBaseMessageConsumer<ServiceRequestOfferAcceptedMessage>
{
    private readonly IServiceProvider _sp;
    public ServiceRequestOfferAcceptedSystemMessageConsumer(IServiceProvider sp) : base(sp) => _sp = sp;

    public override Task<bool> ExecutePrepareMessage(ServiceRequestOfferAcceptedMessage m, CancellationToken ct)
        => Task.FromResult(true);

    public override Task ExecuteCommitMessage(ServiceRequestOfferAcceptedMessage m, CancellationToken ct)
        => _sp.GetRequiredService<ServiceRequestLifecycleMessageWriter>().WriteAsync(
            m.ServiceRequestId, "OFFER_ACCEPTED", MessagingParticipantRole.System, MessageType.StatusChange,
            "OFFER_ACCEPTED", senderUserId: 0, ct);

    public override Task ExecuteRollbackMessage(ServiceRequestOfferAcceptedMessage m, AizenMessageError ex, CancellationToken ct)
    {
        _sp.GetRequiredService<ILogger<ServiceRequestOfferAcceptedSystemMessageConsumer>>()
           .LogWarning("[WC1 lifecycle] rollback OFFER_ACCEPTED SR {SrId}: {Err}", m.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}

/// <summary>JOB_STARTED System pill from <see cref="ServiceRequestAssignmentStartedMessage"/> (WC1-added event).</summary>
public sealed class ServiceRequestAssignmentStartedSystemMessageConsumer
    : AizenBaseMessageConsumer<ServiceRequestAssignmentStartedMessage>
{
    private readonly IServiceProvider _sp;
    public ServiceRequestAssignmentStartedSystemMessageConsumer(IServiceProvider sp) : base(sp) => _sp = sp;

    public override Task<bool> ExecutePrepareMessage(ServiceRequestAssignmentStartedMessage m, CancellationToken ct)
        => Task.FromResult(true);

    public override Task ExecuteCommitMessage(ServiceRequestAssignmentStartedMessage m, CancellationToken ct)
        => _sp.GetRequiredService<ServiceRequestLifecycleMessageWriter>().WriteAsync(
            m.ServiceRequestId, "JOB_STARTED", MessagingParticipantRole.System, MessageType.StatusChange,
            "JOB_STARTED", senderUserId: 0, ct);

    public override Task ExecuteRollbackMessage(ServiceRequestAssignmentStartedMessage m, AizenMessageError ex, CancellationToken ct)
    {
        _sp.GetRequiredService<ILogger<ServiceRequestAssignmentStartedSystemMessageConsumer>>()
           .LogWarning("[WC1 lifecycle] rollback JOB_STARTED SR {SrId}: {Err}", m.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}

/// <summary>JOB_COMPLETED System pill from <see cref="ServiceRequestCompletionApprovedMessage"/>.</summary>
public sealed class ServiceRequestCompletionApprovedSystemMessageConsumer
    : AizenBaseMessageConsumer<ServiceRequestCompletionApprovedMessage>
{
    private readonly IServiceProvider _sp;
    public ServiceRequestCompletionApprovedSystemMessageConsumer(IServiceProvider sp) : base(sp) => _sp = sp;

    public override Task<bool> ExecutePrepareMessage(ServiceRequestCompletionApprovedMessage m, CancellationToken ct)
        => Task.FromResult(true);

    public override Task ExecuteCommitMessage(ServiceRequestCompletionApprovedMessage m, CancellationToken ct)
        => _sp.GetRequiredService<ServiceRequestLifecycleMessageWriter>().WriteAsync(
            m.ServiceRequestId, "JOB_COMPLETED", MessagingParticipantRole.System, MessageType.StatusChange,
            "JOB_COMPLETED", senderUserId: 0, ct);

    public override Task ExecuteRollbackMessage(ServiceRequestCompletionApprovedMessage m, AizenMessageError ex, CancellationToken ct)
    {
        _sp.GetRequiredService<ILogger<ServiceRequestCompletionApprovedSystemMessageConsumer>>()
           .LogWarning("[WC1 lifecycle] rollback JOB_COMPLETED SR {SrId}: {Err}", m.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}

/// <summary>CONVERSATION_CLOSED System pill from <see cref="ServiceRequestCancelledMessage"/>.</summary>
public sealed class ServiceRequestCancelledSystemMessageConsumer
    : AizenBaseMessageConsumer<ServiceRequestCancelledMessage>
{
    private readonly IServiceProvider _sp;
    public ServiceRequestCancelledSystemMessageConsumer(IServiceProvider sp) : base(sp) => _sp = sp;

    public override Task<bool> ExecutePrepareMessage(ServiceRequestCancelledMessage m, CancellationToken ct)
        => Task.FromResult(true);

    public override Task ExecuteCommitMessage(ServiceRequestCancelledMessage m, CancellationToken ct)
        => _sp.GetRequiredService<ServiceRequestLifecycleMessageWriter>().WriteAsync(
            m.ServiceRequestId, "CONVERSATION_CLOSED", MessagingParticipantRole.System, MessageType.StatusChange,
            "CONVERSATION_CLOSED", senderUserId: 0, ct);

    public override Task ExecuteRollbackMessage(ServiceRequestCancelledMessage m, AizenMessageError ex, CancellationToken ct)
    {
        _sp.GetRequiredService<ILogger<ServiceRequestCancelledSystemMessageConsumer>>()
           .LogWarning("[WC1 lifecycle] rollback CONVERSATION_CLOSED SR {SrId}: {Err}", m.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}

/// <summary>
/// The offer CARD from <see cref="ServiceRequestOfferSubmittedMessage"/> (WC1-added submit event). Stored as a
/// Provider-sender StatusChange with content <c>offer:{offerId}|{total:F2} {ccy}</c> — byte-identical to the sync
/// mapping (SR Offer → Messaging StatusChange; the read side detects the <c>offer:</c> content). Keyed
/// <c>sys:{srId}:OFFER:{offerId}</c>. Silent (no notification).
/// </summary>
public sealed class ServiceRequestOfferSubmittedCardConsumer
    : AizenBaseMessageConsumer<ServiceRequestOfferSubmittedMessage>
{
    private readonly IServiceProvider _sp;
    public ServiceRequestOfferSubmittedCardConsumer(IServiceProvider sp) : base(sp) => _sp = sp;

    public override Task<bool> ExecutePrepareMessage(ServiceRequestOfferSubmittedMessage m, CancellationToken ct)
        => Task.FromResult(true);

    public override Task ExecuteCommitMessage(ServiceRequestOfferSubmittedMessage m, CancellationToken ct)
    {
        var content = $"offer:{m.OfferId}|{m.TotalAmount:F2} {m.CurrencyCode}";
        return _sp.GetRequiredService<ServiceRequestLifecycleMessageWriter>().WriteAsync(
            m.ServiceRequestId, $"OFFER:{m.OfferId}", MessagingParticipantRole.Provider, MessageType.StatusChange,
            content, senderUserId: m.ProviderUserId, ct);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestOfferSubmittedMessage m, AizenMessageError ex, CancellationToken ct)
    {
        _sp.GetRequiredService<ILogger<ServiceRequestOfferSubmittedCardConsumer>>()
           .LogWarning("[WC1 lifecycle] rollback OFFER card SR {SrId} offer {OfferId}: {Err}", m.ServiceRequestId, m.OfferId, ex.Message);
        return Task.CompletedTask;
    }
}
