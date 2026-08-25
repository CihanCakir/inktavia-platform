using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>
/// Admin gönderim geçmişinde tek bildirimin TAM satırı: liste özetindeki her şey + gövde/derin-bağlantı/sağlayıcı-ref/
/// referans + ham MetadataJson. MetadataJson ham string olarak döner (FE pretty-print eder). NOT: Email için Sent
/// ötesi teslim izlenmez (SMTP, webhook yok) — API veri destekleyemediği alanı uydurmaz.
/// </summary>
public sealed class NotificationHistoryDetailDto
{
    public long                Id                  { get; init; }
    public long                RecipientUserId     { get; init; }
    public NotificationType    Type                { get; init; }
    public NotificationChannel Channel             { get; init; }
    public string              TemplateCode        { get; init; } = default!;
    public string              Title               { get; init; } = default!;
    public string              Locale              { get; init; } = default!;
    public NotificationStatus  Status              { get; init; }
    /// <summary>Faz 28.6 — bir admin kampanyasından üretildiyse kampanya id'si; aksi halde null.</summary>
    public long?               CampaignId          { get; init; }
    public DateTimeOffset      CreatedAt           { get; init; }
    public DateTimeOffset?     SentAt              { get; init; }
    public DateTimeOffset?     ReadAt              { get; init; }

    public string              Body                { get; init; } = default!;
    public string?             DeepLink            { get; init; }
    public string?             DeliveryProviderRef { get; init; }
    public string?             ReferenceType       { get; init; }
    public long?               ReferenceId         { get; init; }
    /// <summary>Ham metadata JSON (string). FE pretty-print eder; sunucu ayrıştırmaz.</summary>
    public string?             MetadataJson        { get; init; }
}
