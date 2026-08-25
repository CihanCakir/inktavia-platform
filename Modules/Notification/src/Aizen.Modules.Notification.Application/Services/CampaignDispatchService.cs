using System.Text.Json;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Exceptions;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Aizen.Modules.Notification.Domain.Model;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Services;

public sealed class CampaignDispatchService : ICampaignDispatchService
{
    private const int BatchSize = 200;
    private static readonly IReadOnlyDictionary<string, string> EmptyVars = new Dictionary<string, string>();

    private readonly INotificationCampaignRepository _campaignRepository;
    private readonly ICampaignRecipientExpander      _expander;
    private readonly ILocaleResolver                 _localeResolver;
    private readonly ITemplateRenderer               _renderer;
    private readonly ITemplateInterpolator           _interpolator;
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly INotificationRepository         _notificationRepository;
    private readonly INotificationDispatcher         _dispatcher;
    private readonly ILogger<CampaignDispatchService> _logger;

    public CampaignDispatchService(
        INotificationCampaignRepository campaignRepository,
        ICampaignRecipientExpander expander,
        ILocaleResolver localeResolver,
        ITemplateRenderer renderer,
        ITemplateInterpolator interpolator,
        INotificationTemplateRepository templateRepository,
        INotificationRepository notificationRepository,
        INotificationDispatcher dispatcher,
        ILogger<CampaignDispatchService> logger)
    {
        _campaignRepository     = campaignRepository;
        _expander               = expander;
        _localeResolver         = localeResolver;
        _renderer               = renderer;
        _interpolator           = interpolator;
        _templateRepository     = templateRepository;
        _notificationRepository = notificationRepository;
        _dispatcher             = dispatcher;
        _logger                 = logger;
    }

    // Bir (locale,channel) için çözülmüş içerik ya da başarısızlık sebebi (render bir kez yapılıp cache'lenir).
    private sealed record ContentResult(bool Ok, RenderedContent? Content, string? FailureReason);

    public async Task DispatchAsync(long campaignId, CancellationToken ct = default)
    {
        var campaign = await _campaignRepository.GetByIdAsync(campaignId, ct);
        if (campaign is null)
        {
            _logger.LogWarning("Campaign {Id} not found; dispatch skipped.", campaignId);
            return;
        }

        // Idempotency: terminal kampanya tekrar işlenmez (directed-commit zaten exactly-once, bu ek güvenlik).
        if (campaign.Status is CampaignStatus.Completed or CampaignStatus.Failed)
        {
            _logger.LogInformation("Campaign {Id} already terminal ({Status}); dispatch skipped.", campaignId, campaign.Status);
            return;
        }

        // ScheduledAt gelecekteyse: v1'de yalnız SAKLA ve REDDET (poller sonraki fazın bilinçli işi). Kampanya Queued
        // kalır, dağıtım yapılmaz; ileride bir zamanlayıcı zamanı gelince yeniden yayınlayacak.
        if (campaign.ScheduledAt is { } scheduledAt && scheduledAt > DateTimeOffset.UtcNow)
        {
            _logger.LogInformation(
                "Campaign {Id} scheduled for {When} (future); store-and-refuse (poller is a later phase).",
                campaignId, scheduledAt);
            return;
        }

        campaign.MarkProcessing();
        await _campaignRepository.UpdateAsync(campaign, ct);

        var recipients = await ResolveRecipientsAsync(campaign, ct);
        campaign.SetTotalRecipients(recipients.Count);
        await _campaignRepository.UpdateAsync(campaign, ct);

        var channels = ParseChannels(campaign.ChannelsCsv);

        // İçerik yolu: şablon (Type = template.Type) ya da custom (Type = AdminBroadcast).
        var isTemplate = !string.IsNullOrWhiteSpace(campaign.TemplateCode);
        NotificationTemplateEntity? template = null;
        Dictionary<string, CampaignLocaleContent>? custom = null;
        NotificationType rowType;
        if (isTemplate)
        {
            template = await _templateRepository.GetByCodeAsync(campaign.TemplateCode!, ct);
            rowType  = template?.Type ?? NotificationType.AdminBroadcast;
        }
        else
        {
            custom  = DeserializeCustom(campaign.CustomContentJson);
            rowType = NotificationType.AdminBroadcast;
        }

        var rowTemplateCode = campaign.TemplateCode ?? "ADMIN_CAMPAIGN_CUSTOM";

        // (locale,channel) → içerik: her hücre bir kez render edilir (per-locale/channel), satırlar bunu paylaşır.
        var renderCache = new Dictionary<(string Locale, NotificationChannel Channel), ContentResult>();

        foreach (var batch in Chunk(recipients, BatchSize))
        {
            var batchSent = 0;
            var batchFailed = 0;

            // Locale çöz ve grupla — iki alıcı iki farklı locale ise iki ayrı render tetiklenir.
            var byLocale = new Dictionary<string, List<long>>();
            foreach (var rid in batch)
            {
                var locale = (await _localeResolver.ResolveAsync(rid, ct) ?? "en").ToLowerInvariant();
                if (!byLocale.TryGetValue(locale, out var list))
                    byLocale[locale] = list = new List<long>();
                list.Add(rid);
            }

            foreach (var (locale, rids) in byLocale)
            {
                foreach (var channel in channels)
                {
                    var key = (locale, channel);
                    if (!renderCache.TryGetValue(key, out var content))
                    {
                        content = await ResolveContentAsync(isTemplate, template, custom, locale, channel, ct);
                        renderCache[key] = content;
                    }

                    foreach (var rid in rids)
                    {
                        if (!content.Ok)
                        {
                            // Locale için render edilebilir içerik yok → Failed satır (sebep MetadataJson'da).
                            // ASLA sessizce yanlış dilde gönderme.
                            var failMeta = JsonSerializer.Serialize(new { campaignId = campaign.Id, reason = content.FailureReason });
                            var failRow = NotificationEntity.Create(
                                rid, rowType, channel, rowTemplateCode,
                                title: "—", body: content.FailureReason ?? "locale içerik yok",
                                metadataJson: failMeta, referenceType: null, referenceId: null,
                                locale: locale, deepLink: null, campaignId: campaign.Id);
                            failRow.MarkAsFailed();
                            await _notificationRepository.AddAsync(failRow, ct);
                            batchFailed++;
                            continue;
                        }

                        var meta = JsonSerializer.Serialize(new { campaignId = campaign.Id });
                        var entity = NotificationEntity.Create(
                            rid, rowType, channel, rowTemplateCode,
                            content.Content!.Title, content.Content.Body,
                            metadataJson: meta, referenceType: null, referenceId: null,
                            locale: locale, deepLink: content.Content.DeepLink, campaignId: campaign.Id);

                        await _notificationRepository.AddAsync(entity, ct);

                        try
                        {
                            await _dispatcher.DispatchAsync(entity, ct);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Campaign {Cid} dispatch failed for NotificationId={Nid}", campaign.Id, entity.Id);
                            entity.MarkAsFailed();
                            await _notificationRepository.UpdateAsync(entity, ct);
                        }

                        if (entity.Status == NotificationStatus.Sent) batchSent++;
                        else batchFailed++;
                    }
                }
            }

            // Sayaçlar batch başına güncellenir.
            campaign.AddBatchResult(batchSent, batchFailed);
            await _campaignRepository.UpdateAsync(campaign, ct);
        }

        // Terminal: her satır başarısızsa Failed; aksi halde Completed. KISMİ başarı da Completed'dır — sayaçlar
        // gerçeği taşır, sessiz yeniden gönderim yapılmaz.
        var totalRows = campaign.SentCount + campaign.FailedCount;
        if (totalRows > 0 && campaign.SentCount == 0)
            campaign.Fail();
        else
            campaign.Complete();

        await _campaignRepository.UpdateAsync(campaign, ct);

        _logger.LogInformation(
            "Campaign {Id} finished: status={Status} recipients={Total} sent={Sent} failed={Failed}.",
            campaign.Id, campaign.Status, campaign.TotalRecipients, campaign.SentCount, campaign.FailedCount);
    }

    private async Task<ContentResult> ResolveContentAsync(
        bool isTemplate, NotificationTemplateEntity? template,
        Dictionary<string, CampaignLocaleContent>? custom,
        string locale, NotificationChannel channel, CancellationToken ct)
    {
        try
        {
            if (isTemplate)
            {
                if (template is null)
                    return new ContentResult(false, null, "şablon yok");

                // v1: per-recipient değişken yok; şablon self-contained olmalı. Placeholder varsa strict render
                // TemplatePlaceholderMissingException atar → bu (locale,channel) Failed olur.
                var rendered = await _renderer.RenderAsync(template.TemplateCode, channel, locale, EmptyVars, ct);
                return new ContentResult(true, rendered, null);
            }

            // Custom yol: locale'e ait {title,body} birebir kullanılır (placeholder içeriyorsa yine strict interpolasyon).
            if (custom is null || !custom.TryGetValue(locale, out var c))
                return new ContentResult(false, null, "locale içerik yok");

            var title = _interpolator.TryInterpolateStrict(c.Title, EmptyVars, out var missTitle);
            var body  = _interpolator.TryInterpolateStrict(c.Body, EmptyVars, out var missBody);
            if (title is null || body is null)
            {
                var missing = missTitle.Concat(missBody).Distinct();
                return new ContentResult(false, null, $"eksik placeholder: {string.Join(",", missing)}");
            }

            return new ContentResult(true, new RenderedContent(title, body, null), null);
        }
        catch (TemplateContentMissingException)
        {
            return new ContentResult(false, null, "locale içerik yok");
        }
        catch (TemplatePlaceholderMissingException ex)
        {
            return new ContentResult(false, null, $"eksik placeholder: {string.Join(",", ex.MissingKeys)}");
        }
    }

    private async Task<IReadOnlyList<long>> ResolveRecipientsAsync(NotificationCampaignEntity campaign, CancellationToken ct)
    {
        if (campaign.TargetMode == CampaignTargetMode.Selected)
        {
            var ids = JsonSerializer.Deserialize<List<long>>(campaign.SelectedRecipientIdsJson ?? "[]") ?? new List<long>();
            return ids.Where(x => x > 0).Distinct().ToList();
        }

        return await _expander.ExpandAsync(campaign.Audience, ct);
    }

    private static Dictionary<string, CampaignLocaleContent>? DeserializeCustom(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        var raw = JsonSerializer.Deserialize<Dictionary<string, CampaignLocaleContent>>(json);
        return raw is null ? null : new Dictionary<string, CampaignLocaleContent>(raw, StringComparer.OrdinalIgnoreCase);
    }

    private static List<NotificationChannel> ParseChannels(string csv)
        => csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
              .Select(s => (NotificationChannel)int.Parse(s))
              .Distinct()
              .ToList();

    private static IEnumerable<List<T>> Chunk<T>(IReadOnlyList<T> source, int size)
    {
        for (var i = 0; i < source.Count; i += size)
            yield return source.Skip(i).Take(size).ToList();
    }
}
