# Content → Notification integration contract

> **Boundary (B2).** Content never dispatches push/in-app notifications itself. On publish/unpublish it
> emits an integration event on the message bus; the **Notification** module may consume it and decide
> what to deliver. This document is the contract Notification consumes. **Wiring the consumer into
> Notification is a follow-up owned by the Notification module and is out of scope for the Content build.**

---

## 1. Message shapes (from `Content.Abstraction/Message`, shipped in C3)

Both extend `Aizen.Core.Messagebus.Abstraction.Messages.AizenBaseMessage`. Enum-derived fields travel as
their **string names** (forward-compatible); timestamps are `DateTimeOffset`.

```csharp
public sealed class ContentPublishedMessage : AizenBaseMessage
{
    public string         ContentId    { get; set; }   // Mongo id of the content item
    public string         Slug         { get; set; }   // URL-safe unique slug
    public string         Type         { get; set; }   // ContentType name, e.g. "Blog" | "Announcement" | "Campaign" | "ReleaseNote" | "ProductPromo" | "Faq"
    public string[]       Surfaces     { get; set; }   // ContentSurface names, e.g. ["MarineOsWeb","Provider"]
    public string         AudienceType { get; set; }   // ContentAudienceType name: "Public" | "Providers" | "VesselOwners" | "Segment"
    public DateTimeOffset PublishedAt  { get; set; }
}

public sealed class ContentUnpublishedMessage : AizenBaseMessage
{
    public string         ContentId     { get; set; }
    public string         Slug          { get; set; }
    public string         Type          { get; set; }
    public string[]       Surfaces      { get; set; }
    public string         AudienceType  { get; set; }
    public DateTimeOffset UnpublishedAt { get; set; }
}
```

### When each is emitted (Content, best-effort)

| Handler | Emits | Condition |
| --- | --- | --- |
| `PublishContentItemCommandHandler` | `ContentPublishedMessage` | on every successful publish |
| `UnpublishContentItemCommandHandler` | `ContentUnpublishedMessage` | on every successful unpublish |
| `ArchiveContentItemCommandHandler` | `ContentUnpublishedMessage` | only when the item **was Published** |
| `DeleteContentItemCommandHandler` | `ContentUnpublishedMessage` | only when the item **was Published** |

All publishes are **best-effort**: wrapped in `try/catch` and logged at `Warning`; a bus hiccup never
fails the content write (the DB state is authoritative; consumers are eventually-consistent).

---

## 2. Bus routing / topology

Content publishes through `IAizenMessagePublisher` (`Aizen.Core.Messagebus.AizenMessagePublisher`):

```csharp
await _publisher.PublishAsync(new ContentPublishedMessage { ... }, ct);
```

Internally this is the platform's **two-phase (prepare / commit / rollback)** bus. `PublishAsync` wraps the
payload as `AizenPrepareMessage<ContentPublishedMessage>` and calls MassTransit's
`IPublishEndpoint.Publish(...)`. So on the wire the routed message type is
**`AizenPrepareMessage<ContentPublishedMessage>`** (MassTransit type-based exchange, i.e. the exchange keyed
to that closed generic type), not the bare `ContentPublishedMessage`. Consumers do **not** subscribe to the
raw type — they inherit the base consumer, which handles the `AizenPrepareMessage<T>` / commit / rollback
envelope for them (identical to how `CargoDry*` and `ServiceRequestPublished` messages are consumed today).

No exchange/queue names need to be hard-coded on the Content side — MassTransit derives them from the
message type; the Notification consumer's registration binds the queue automatically.

---

## 3. Recommended consumer skeleton (Notification module — example only)

Mirror `Notification/Consumers/CargoDry/CargoDryKitActivatedConsumer` and
`Notification/Consumers/ServiceRequest/ServiceRequestPublishedConsumer`. Inherit
`AizenBaseMessageConsumer<ContentPublishedMessage>` (from `Aizen.Core.Messagebus.Abstraction.Consumers`):

```csharp
// FILE (future, in Notification): Consumers/Content/ContentPublishedConsumer.cs
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Content.Abstraction.Message;      // Content.Abstraction ref (sanctioned cross-module)
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public sealed class ContentPublishedConsumer : AizenBaseMessageConsumer<ContentPublishedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ContentPublishedConsumer> _logger;

    public ContentPublishedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ContentPublishedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ContentPublishedMessage m, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ContentPublishedMessage m, CancellationToken ct)
    {
        // Broadcast: the event has NO recipient (see §4). Notification resolves the audience → recipients
        // (e.g. Public → web/marketing only; Providers/VesselOwners/Segment → fan out via Identity), then:
        foreach (var recipientUserId in await ResolveRecipientsAsync(m, ct))
        {
            await _sender.Send(new SendNotificationCommand
            {
                RecipientUserId = recipientUserId,
                Type            = NotificationType.ContentPublished,   // recommended new type — see §5
                Channel         = NotificationChannel.InApp,
                Variables = new Dictionary<string, string>
                {
                    ["contentType"] = m.Type,
                    ["slug"]        = m.Slug,
                },
                MetadataJson  = $"{{\"slug\":\"{m.Slug}\",\"surfaces\":\"{string.Join(",", m.Surfaces)}\"}}",
                ReferenceType = "Content",
                // ReferenceId is a long in SendNotificationCommand; ContentId is a Mongo string —
                // carry the id via MetadataJson/slug instead (see §4 gap #2).
            }, ct);
        }
        _logger.LogInformation("ContentPublished fan-out for slug={Slug} type={Type}", m.Slug, m.Type);
    }

    public override Task ExecuteRollbackMessage(ContentPublishedMessage m, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback ContentPublishedConsumer slug={Slug}: {Error}", m.Slug, ex.Message);
        return Task.CompletedTask;
    }
}
```

`ContentUnpublishedMessage` would get an analogous consumer if Notification wants to retract/adjust
(usually a no-op — most platforms don't notify on unpublish).

---

## 4. Contract gaps / recommendations (do NOT change the shipped Content message without agreement)

1. **The event is a broadcast — it carries no recipient.** `ContentPublishedMessage` intentionally holds
   `Surfaces[]` + `AudienceType` (where / who-class), **not** a `RecipientUserId`. Resolving audience →
   concrete recipients is **Notification's responsibility**, exactly as `ServiceRequestPublishedConsumer`
   fans out to region providers via `INotificationIdentityRemoteCall`. Keeping recipient resolution out of
   Content is deliberate (B2 loose coupling, B5 no geo/audience expansion in Content). **Recommendation:**
   Notification maps `AudienceType`/`Surfaces` to a delivery policy (e.g. `Public` → marketing/web channels
   only, no per-user push; `Providers`/`VesselOwners`/`Segment` → Identity-driven fan-out). Do **not** add
   recipient fields to the Content message.
2. **`ContentId` is a string; `SendNotificationCommand.ReferenceId` is a `long`.** Content ids are Mongo
   ObjectIds, so they don't fit `ReferenceId`. **Recommendation:** carry `Slug` (and `ContentId`) in
   `MetadataJson` for deep-linking; leave `ReferenceId` unset (or add a `ReferenceCode`/string ref to the
   Notification contract if a first-class string reference is wanted — a Notification-side change).
3. **No `Title` in the event.** The message is minimal by design (stable contract). If a notification body
   needs the localized title, the consumer should read it via a Content read path (public by-slug) or
   Content could add a `DefaultTitle` field **only if agreed** — this would be an additive, backward-
   compatible change to `ContentPublishedMessage`, called out here rather than made unilaterally.

---

## 5. Recommended `NotificationType` + template (Notification-owned)

`NotificationType` currently ends at `CargoDryKitRevoked = 304` with no Content values. Suggested:

```csharp
// Add to Notification.Abstraction/Enum/NotificationType.cs (Notification's change, not Content's)
ContentPublished = 400,   // "New content published" announcement
```

Template mapping (Notification's `NotificationTemplate` seed), e.g.:

| Type | Channel | Title (tr) | Body (tr) | Variables |
| --- | --- | --- | --- | --- |
| `ContentPublished` | `InApp` | "Yeni içerik yayında" | "{contentType} yayınlandı." | `contentType`, `slug` |

---

## 6. Out of scope (explicit)

- **No Notification code is added or modified in the Content build.** The consumer above is an example.
- Adding `ContentPublishedConsumer`, the `NotificationType.ContentPublished` value, the template seed, and
  the audience→recipient resolver are **Notification-module follow-ups**.
- Content's only obligation — already met — is to publish the stable `ContentPublishedMessage` /
  `ContentUnpublishedMessage` contracts on the bus, best-effort.
