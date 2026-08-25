using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>
/// Admin gönderim geçmişi listesinde tek satır özeti. Body/metadata AĞIR olduğundan listeye DAHİL EDİLMEZ —
/// tam gövde için detay ucu (<see cref="NotificationHistoryDetailDto"/>) kullanılır.
/// </summary>
public sealed class NotificationHistoryListItemDto
{
    public long                Id              { get; init; }
    public long                RecipientUserId { get; init; }
    public NotificationType    Type            { get; init; }
    public NotificationChannel Channel         { get; init; }
    public string              TemplateCode    { get; init; } = default!;
    public string              Title           { get; init; } = default!;
    public string              Locale          { get; init; } = default!;
    public NotificationStatus  Status          { get; init; }
    /// <summary>Faz 28.6 — bir admin kampanyasından üretildiyse kampanya id'si; aksi halde null.</summary>
    public long?               CampaignId      { get; init; }
    public DateTimeOffset      CreatedAt       { get; init; }
    public DateTimeOffset?     SentAt          { get; init; }
    public DateTimeOffset?     ReadAt          { get; init; }
}
