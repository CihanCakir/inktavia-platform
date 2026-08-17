using Aizen.Core.Cache.Abstraction;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Content.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.RemoteCall;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.Content;

/// <summary>
/// Consumes the Content module's <see cref="ContentPublishedMessage"/> broadcast (per
/// docs/CONTENT_NOTIFICATION_CONTRACT.md) and dispatches a <c>ContentPublished</c> in-app awareness
/// notification.
///
/// Recipient resolution (v1). The event is a broadcast — it carries only <c>Surfaces[]</c> +
/// <c>AudienceType</c>, no recipient and (deliberately, Content B5) no region/city codes. The Identity
/// read-model (<see cref="INotificationIdentityRemoteCall"/>) exposes only a city-keyed provider resolver
/// (<c>GetProvidersForArea</c>) and an admin resolver (<c>GetAdminUserIds</c>); there is no bounded
/// "all providers / all vessel owners" resolver, and the event carries no city to feed the area resolver.
/// So v1 fans out to <b>platform admins only</b> (bounded, real, mirrors the N-D admin fan-out) for
/// awareness, regardless of audience type:
///   • Public                         → no end-user blast (unbounded); admins only.
///   • Providers / VesselOwners / Segment → end-user targeting DEFERRED (contract gap, see report); admins only.
/// This never produces an unbounded fan-out. Broad end-user targeting is unblocked only by either region
/// codes on the event or a bulk Identity resolver — neither is changed here.
///
/// Idempotency: redelivery-safe via a per-ContentId "announced" marker in the distributed cache.
/// Per-recipient dispatch is best-effort (one failure never poisons the whole message).
/// </summary>
public sealed class ContentPublishedConsumer : AizenBaseMessageConsumer<ContentPublishedMessage>
{
    private const int AdminRecipientCap = 500;
    private static readonly TimeSpan AnnouncedTtl = TimeSpan.FromDays(7);

    private readonly ISender _sender;
    private readonly INotificationIdentityRemoteCall _identity;
    private readonly IAizenDistributedCache _cache;
    private readonly ILogger<ContentPublishedConsumer> _logger;

    public ContentPublishedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender   = sp.GetRequiredService<ISender>();
        _identity = sp.GetRequiredService<INotificationIdentityRemoteCall>();
        _cache    = sp.GetRequiredService<IAizenDistributedCache>();
        _logger   = sp.GetRequiredService<ILogger<ContentPublishedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ContentPublishedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ContentPublishedMessage message, CancellationToken ct)
    {
        // ── Idempotency: skip if this content id was already announced (redelivery-safe). ─────────────
        var announcedKey = $"content:published:announced:{message.ContentId}";
        if (await _cache.ExistNoHash(announcedKey))
        {
            _logger.LogInformation("ContentPublished {ContentId} ({Slug}) already announced; skipping redelivery.",
                message.ContentId, message.Slug);
            return;
        }

        LogAudiencePolicy(message);

        // ── Resolve the bounded awareness recipient set (admins). A resolver failure leaves the marker
        //    unset so the message can be retried — do not swallow into a permanent skip. ──────────────
        List<long> adminIds;
        try
        {
            adminIds = (await _identity.GetAdminUserIds()).Body ?? new List<long>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ContentPublished {ContentId}: admin resolution failed; nothing announced (will retry on redelivery).",
                message.ContentId);
            return;
        }

        if (adminIds.Count > AdminRecipientCap)
        {
            _logger.LogWarning("ContentPublished {ContentId}: {Total} admins exceeds cap {Cap}; truncating.",
                message.ContentId, adminIds.Count, AdminRecipientCap);
            adminIds = adminIds.Take(AdminRecipientCap).ToList();
        }

        var metadataJson =
            $"{{\"contentId\":\"{message.ContentId}\",\"slug\":\"{message.Slug}\",\"type\":\"{message.Type}\"," +
            $"\"surfaces\":\"{string.Join(",", message.Surfaces)}\",\"audience\":\"{message.AudienceType}\"}}";

        // ── Dispatch sequentially (single-scoped-DbContext pattern), best-effort per recipient. ───────
        var sent = 0;
        foreach (var adminUserId in adminIds.Distinct())
        {
            try
            {
                await _sender.Send(new SendNotificationCommand
                {
                    RecipientUserId = adminUserId,
                    Type            = NotificationType.ContentPublished,
                    Channel         = NotificationChannel.InApp,   // InApp is the always-on baseline (N-B not gated)
                    Variables = new Dictionary<string, string>
                    {
                        ["contentType"] = message.Type,
                        ["slug"]        = message.Slug,
                    },
                    MetadataJson  = metadataJson,
                    ReferenceType = "Content",
                    // ReferenceId is a long; ContentId is a Mongo string, so it is carried in MetadataJson (contract gap #2).
                }, ct);
                sent++;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "ContentPublished {ContentId}: failed to notify admin {AdminUserId}; continuing.",
                    message.ContentId, adminUserId);
            }
        }

        // ── Mark announced only after a completed dispatch pass, so a transient failure above can retry. ─
        await _cache.SetNoHash(announcedKey, "1", AnnouncedTtl);

        _logger.LogInformation(
            "ContentPublished fan-out for slug={Slug} type={Type} audience={Audience}: notified {Sent}/{Total} admins.",
            message.Slug, message.Type, message.AudienceType, sent, adminIds.Count);
    }

    public override Task ExecuteRollbackMessage(ContentPublishedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ContentPublishedConsumer for {ContentId} ({Slug}): {Error}",
            message.ContentId, message.Slug, ex.Message);
        return Task.CompletedTask;
    }

    private void LogAudiencePolicy(ContentPublishedMessage message)
    {
        if (string.Equals(message.AudienceType, "Public", StringComparison.OrdinalIgnoreCase))
            _logger.LogDebug(
                "ContentPublished {ContentId}: Public audience — end-user fan-out intentionally skipped " +
                "(unbounded blast radius); admin awareness only.", message.ContentId);
        else
            _logger.LogDebug(
                "ContentPublished {ContentId}: audience={Audience} — end-user targeting deferred " +
                "(no bounded Identity resolver + event carries no region codes); admin awareness only.",
                message.ContentId, message.AudienceType);
    }
}
