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
    /// <summary>Routable entity kind for deep-link (e.g. "ServiceRequest","Job","Message","CargoDry","Payment","Milestone"). Nullable.</summary>
    public string?             ReferenceType       { get; private set; }
    /// <summary>Id of the referenced entity for deep-link. Nullable.</summary>
    public long?               ReferenceId         { get; private set; }
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
        string? metadataJson = null,
        string? referenceType = null,
        long? referenceId = null)
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
            ReferenceType   = referenceType,
            ReferenceId     = referenceId,
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
        DateTimeOffset? readAtUtc,
        string? referenceType = null,
        long? referenceId = null)
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
            ReferenceType   = referenceType,
            ReferenceId     = referenceId,
            // FAZ16 (#34): seed satırları "iletilmiş" bildirimleri temsil eder → Status daima Sent (gönderim durumu).
            // Okunmuşluk ayrı kolon ReadAt ile taşınır; Status artık okundu diye Read'e ezilmez.
            Status          = NotificationStatus.Sent,
            CreatedAt       = createdAtUtc,
            SentAt          = createdAtUtc,
            ReadAt          = readAtUtc,
        };
    }

    // FAZ15 (#34) — DEĞİŞMEZ: Pending satırı = e-posta HİÇ gönderilmedi.
    // Gönderim denemesi transport'a gitmeden ÖNCE satır bu metotla Pending'ten çıkarılıp KALICI yazılır; böylece
    // SendMailAsync başarıyla dönüp sonucu (Sent/Failed) yazılamadan çökme olursa satır 'Sending' kalır (denendi,
    // sonuç belirsiz) — asla "hiç denenmemiş" gibi görünen Pending değil.
    public void MarkAsSending() => Status = NotificationStatus.Sending;

    public void MarkAsSent(string? providerRef = null)
    {
        Status              = NotificationStatus.Sent;
        SentAt              = DateTimeOffset.UtcNow;
        DeliveryProviderRef = providerRef;
    }

    public void MarkAsFailed() => Status = NotificationStatus.Failed;

    // FAZ16 (#34) — okunmuşluk ile GÖNDERİM DURUMU ayrı gerçeklerdir; artık aynı kolonu paylaşmazlar.
    // Okunmuşluk YALNIZ ReadAt ile taşınır (IsRead + liste/sayaç sorguları hep ReadAt'e bakar). Status yalnız
    // gönderim yaşam döngüsünü (Pending/Sending/Sent/Failed) tutar. Okundu diye Status'ü Read'e EZMEK "iletildi mi?"
    // gerçeğini yok ederdi (bu fazın tüm amacı onu korumak). Read=4 enum değeri eski satırlar için geride kalır ama
    // artık runtime'da YAZILMAZ.
    public void MarkAsRead()
    {
        if (IsRead) return;
        ReadAt = DateTimeOffset.UtcNow;
    }

    public bool IsRead => ReadAt.HasValue;

    public void RedactBody(string redactedBody) => Body = redactedBody;
}
