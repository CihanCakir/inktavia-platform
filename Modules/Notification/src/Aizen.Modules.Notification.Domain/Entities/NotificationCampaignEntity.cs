using Aizen.Core.Domain;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Domain.Entities;

/// <summary>
/// Admin tarafından oluşturulan doğrudan/toplu bildirim kampanyası. Yeni bir gönderim yolu DEĞİL — recipient başına
/// mevcut kanal dispatcher'ları üzerinden (RRabbitMQ consumer) dağıtılır. Her gönderilen NotificationEntity satırı
/// bu kampanyaya CampaignId ile bağlanır.
/// </summary>
public sealed class NotificationCampaignEntity : AizenEntity
{
    public CampaignAudience   Audience                { get; private set; }
    public CampaignTargetMode TargetMode              { get; private set; }
    /// <summary>TargetMode=Selected için alıcı id listesi (JSON long[]). All modunda null.</summary>
    public string?            SelectedRecipientIdsJson { get; private set; }
    /// <summary>Şablon yolu: gönderilecek template kodu. Custom yolda null.</summary>
    public string?            TemplateCode            { get; private set; }
    /// <summary>Custom yol: locale→{title,body} haritası (JSON). Şablon yolunda null.</summary>
    public string?            CustomContentJson       { get; private set; }
    /// <summary>
    /// Kanallar CSV (NotificationChannel int değerleri, ör. "1,3"). NotificationChannel bir [Flags] enum değil
    /// (power-of-two değil) → bit-maske yerine repo-dostu en basit yol: virgülle ayrılmış int CSV.
    /// </summary>
    public string             ChannelsCsv             { get; private set; } = default!;
    public CampaignStatus     Status                  { get; private set; }
    public int                TotalRecipients         { get; private set; }
    public int                SentCount               { get; private set; }
    public int                FailedCount             { get; private set; }
    public DateTimeOffset?    ScheduledAt             { get; private set; }
    public long               CreatedByUserId         { get; private set; }
    public DateTimeOffset     CreatedAt               { get; private set; }
    public DateTimeOffset?    CompletedAt             { get; private set; }

    private NotificationCampaignEntity() { }

    public static NotificationCampaignEntity Create(
        CampaignAudience audience,
        CampaignTargetMode targetMode,
        string? selectedRecipientIdsJson,
        string? templateCode,
        string? customContentJson,
        string channelsCsv,
        DateTimeOffset? scheduledAt,
        long createdByUserId)
    {
        return new NotificationCampaignEntity
        {
            Audience                 = audience,
            TargetMode               = targetMode,
            SelectedRecipientIdsJson = selectedRecipientIdsJson,
            TemplateCode             = templateCode,
            CustomContentJson        = customContentJson,
            ChannelsCsv              = channelsCsv,
            // Oluşturulur oluşturulmaz kuyruğa alınır (mesaj yayınlanacak); ScheduledAt geleceyse consumer store-and-refuse eder.
            Status                   = CampaignStatus.Queued,
            TotalRecipients          = 0,
            SentCount                = 0,
            FailedCount              = 0,
            ScheduledAt              = scheduledAt,
            CreatedByUserId          = createdByUserId,
            CreatedAt                = DateTimeOffset.UtcNow,
        };
    }

    public void MarkProcessing() => Status = CampaignStatus.Processing;

    public void SetTotalRecipients(int total) => TotalRecipients = total;

    /// <summary>Bir batch işlendikten sonra sayaçları artırır (batch başına güncelleme).</summary>
    public void AddBatchResult(int sent, int failed)
    {
        SentCount   += sent;
        FailedCount += failed;
    }

    /// <summary>Terminal başarı. Kısmi başarı da Completed'dır (sayaçlar gerçeği taşır) — sessiz yeniden gönderim yok.</summary>
    public void Complete()
    {
        Status      = CampaignStatus.Completed;
        CompletedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>Terminal başarısızlık: hiçbir satır gönderilemedi.</summary>
    public void Fail()
    {
        Status      = CampaignStatus.Failed;
        CompletedAt = DateTimeOffset.UtcNow;
    }
}
