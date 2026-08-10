namespace Aizen.Modules.Messaging.Application.Configuration;

/// <summary>
/// BE_WC0 — Phase-4 write-cutover feature flags (config section <c>Messaging:WriteCutover</c>). Scaffolding only:
/// all default OFF and are bound-but-unused this phase. Each phase flips exactly one flag, parallel-then-flip:
/// <list type="bullet">
///   <item><see cref="GenerateSystemMessages"/> — WC1: generate System/lifecycle messages inside Messaging from SR
///   domain events (idempotent on the <c>sys:{srId}:{code}</c> SourceKey), in parallel with the SR writer.</item>
///   <item><see cref="WriteMessagesToMessaging"/> — WC2: participant sends write to the Messaging store as the source
///   of truth.</item>
///   <item><see cref="ReadMessagesFromMessaging"/> — WC3: the <c>sr.Messages</c> readers repoint to the Messaging
///   store (see the reader inventory in the WC0 report).</item>
///   <item><see cref="DisableServiceRequestMessageSync"/> — WC4: retire the one-directional sync consumer + its
///   per-SR semaphore once the DB unique index is the sole idempotency guard.</item>
/// </list>
/// </summary>
public sealed class MessagingWriteCutoverOptions
{
    /// <summary>Config section path.</summary>
    public const string SectionName = "Messaging:WriteCutover";

    public bool GenerateSystemMessages { get; set; }
    public bool WriteMessagesToMessaging { get; set; }
    public bool ReadMessagesFromMessaging { get; set; }
    public bool DisableServiceRequestMessageSync { get; set; }
}
