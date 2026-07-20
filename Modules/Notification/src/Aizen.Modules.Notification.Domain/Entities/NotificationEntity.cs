using Aizen.Core.Domain;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Domain.Entities;

public sealed class NotificationEntity : AizenEntity
{
    public long                RecipientUserId     { get; private set; }
    public NotificationType    Type                { get; private set; }
    public NotificationChannel Channel             { get; private set; }
    public string              TemplateCode        { get; private set; } = default!;
    public string              Title               { get; private set; } = default!;
    public string              Body                { get; private set; } = default!;
    public NotificationStatus  Status              { get; private set; }
    public string?             DeliveryProviderRef { get; private set; }
    public string?             MetadataJson        { get; private set; }
    public DateTimeOffset      CreatedAt           { get; private set; }
    public DateTimeOffset?     SentAt              { get; private set; }
    public DateTimeOffset?     ReadAt              { get; private set; }

    private NotificationEntity() { }

    public static NotificationEntity Create(
        long recipientUserId,
        NotificationType type,
        NotificationChannel channel,
        string templateCode,
        string title,
        string body,
        string? metadataJson = null)
    {
        return new NotificationEntity
        {
            RecipientUserId = recipientUserId,
            Type            = type,
            Channel         = channel,
            TemplateCode    = templateCode,
            Title           = title,
            Body            = body,
            Status          = NotificationStatus.Pending,
            MetadataJson    = metadataJson,
            CreatedAt       = DateTimeOffset.UtcNow,
        };
    }

    /// <summary>Dev/seed only: create with explicit CreatedAt/ReadAt for realistic display seeding.</summary>
    public static NotificationEntity CreateSeed(
        long recipientUserId,
        NotificationType type,
        NotificationChannel channel,
        string templateCode,
        string title,
        string body,
        string? metadataJson,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? readAtUtc)
    {
        return new NotificationEntity
        {
            RecipientUserId = recipientUserId,
            Type            = type,
            Channel         = channel,
            TemplateCode    = templateCode,
            Title           = title,
            Body            = body,
            MetadataJson    = metadataJson,
            Status          = readAtUtc.HasValue ? NotificationStatus.Read : NotificationStatus.Sent,
            CreatedAt       = createdAtUtc,
            SentAt          = createdAtUtc,
            ReadAt          = readAtUtc,
        };
    }

    public void MarkAsSent(string? providerRef = null)
    {
        Status              = NotificationStatus.Sent;
        SentAt              = DateTimeOffset.UtcNow;
        DeliveryProviderRef = providerRef;
    }

    public void MarkAsFailed() => Status = NotificationStatus.Failed;

    public void MarkAsRead()
    {
        if (Status == NotificationStatus.Read) return;
        Status = NotificationStatus.Read;
        ReadAt = DateTimeOffset.UtcNow;
    }

    public bool IsRead => ReadAt.HasValue;

    public void RedactBody(string redactedBody) => Body = redactedBody;
}
