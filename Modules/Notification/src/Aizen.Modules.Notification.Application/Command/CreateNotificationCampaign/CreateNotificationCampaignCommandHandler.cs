using System.Text.Json;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Message;
using Aizen.Modules.Notification.Abstraction.Request;
using Aizen.Modules.Notification.Abstraction.Response;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Application.Command.CreateNotificationCampaign;

public sealed class CreateNotificationCampaignCommandHandler
    : AizenCommandHandler<CreateNotificationCampaignCommand, NotificationCampaignMutationResponse>
{
    // Kampanya kanalları yalnız InApp ve/veya Email olabilir (Sms/Push Faz 7'ye kadar reddedilir).
    private static readonly NotificationChannel[] AllowedChannels = { NotificationChannel.InApp, NotificationChannel.Email };

    private readonly INotificationCampaignRepository        _campaignRepository;
    private readonly INotificationTemplateRepository        _templateRepository;
    private readonly INotificationTemplateContentRepository _contentRepository;
    private readonly IAizenInfoAccessor                     _info;
    private readonly IAizenMessagePublisher                 _publisher;
    private readonly ILogger<CreateNotificationCampaignCommandHandler> _logger;

    public CreateNotificationCampaignCommandHandler(
        INotificationCampaignRepository campaignRepository,
        INotificationTemplateRepository templateRepository,
        INotificationTemplateContentRepository contentRepository,
        IAizenInfoAccessor info,
        IAizenMessagePublisher publisher,
        ILogger<CreateNotificationCampaignCommandHandler> logger)
    {
        _campaignRepository = campaignRepository;
        _templateRepository = templateRepository;
        _contentRepository  = contentRepository;
        _info               = info;
        _publisher          = publisher;
        _logger             = logger;
    }

    public override async Task<NotificationCampaignMutationResponse?> Handle(
        CreateNotificationCampaignCommand request, CancellationToken cancellationToken)
    {
        var channels = ValidateChannels(request.Channels);
        var (templateCode, customContentJson) = await ValidateContentPathAsync(request, channels, cancellationToken);
        var selectedJson = ValidateAndSerializeRecipients(request);

        var channelsCsv = string.Join(",", channels.Select(c => ((int)c).ToString()));
        var createdByUserId = _info.UserInfoAccessor.UserInfo.UserId;

        var campaign = NotificationCampaignEntity.Create(
            request.Audience, request.TargetMode, selectedJson, templateCode, customContentJson,
            channelsCsv, request.ScheduledAt, createdByUserId);

        await _campaignRepository.AddAsync(campaign, cancellationToken);

        // Kuyruğa al: CampaignDispatchConsumer bu id ile dağıtır. Yayın hatası kampanya yazımını geri almamalı
        // (best-effort) — kampanya Queued kalır, tekrar yayınlanabilir; poller sonraki faz.
        try
        {
            await _publisher.PublishAsync(
                new NotificationCampaignQueuedMessage { CampaignId = campaign.Id }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to publish NotificationCampaignQueuedMessage for CampaignId={Id}; campaign stays Queued.",
                campaign.Id);
        }

        return new NotificationCampaignMutationResponse { CampaignId = campaign.Id, Status = campaign.Status };
    }

    private static List<NotificationChannel> ValidateChannels(List<NotificationChannel> requested)
    {
        var channels = requested.Distinct().ToList();
        if (channels.Count == 0)
            throw new AizenBusinessException("En az bir kanal seçilmelidir.");

        // Sms/Push reddedilir (yalnız InApp/Email). "Sms refused until Phase 7".
        var invalid = channels.Where(c => !AllowedChannels.Contains(c)).ToList();
        if (invalid.Count > 0)
            throw new AizenBusinessException(
                $"Kampanya kanalları yalnız InApp ve/veya Email olabilir. Reddedilen: {string.Join(", ", invalid)}.");

        return channels;
    }

    /// <summary>Tam olarak BİR içerik yolu (şablon YA DA custom). Döner: (kanonik templateCode?, customContentJson?).</summary>
    private async Task<(string? TemplateCode, string? CustomContentJson)> ValidateContentPathAsync(
        CreateNotificationCampaignCommand request, List<NotificationChannel> channels, CancellationToken ct)
    {
        var hasTemplate = !string.IsNullOrWhiteSpace(request.TemplateCode);
        var hasCustom   = request.CustomContent is { Count: > 0 };

        if (hasTemplate == hasCustom)
            throw new AizenBusinessException("Tam olarak bir içerik yolu belirtilmelidir: şablon YA DA custom içerik.");

        if (hasTemplate)
        {
            var template = await _templateRepository.GetByCodeAsync(request.TemplateCode!, ct)
                           ?? throw new AizenBusinessException($"Template '{request.TemplateCode}' bulunamadı.");

            // Şablon yolu: desteklenen dillerden EN AZ BİRİ için (seçilen kanallarda) yayınlanmış içerik olmalı.
            var anyPublished = false;
            foreach (var channel in channels)
            {
                foreach (var locale in CampaignLocales.Supported)
                {
                    if (await _contentRepository.GetCurrentPublishedAsync(template.Id, channel, locale, ct) is not null)
                    {
                        anyPublished = true;
                        break;
                    }
                }
                if (anyPublished) break;
            }

            if (!anyPublished)
                throw new AizenBusinessException(
                    $"Template '{template.TemplateCode}' için desteklenen dillerde (tr/en) yayınlanmış içerik yok.");

            return (template.TemplateCode, null);
        }

        // Custom yol: desteklenen TÜM diller kapsanmalı (madde: kısmi-locale custom send yasak — sessiz yanlış-dil
        // göndermeyi engeller). Locale anahtarları büyük/küçük harf duyarsız, saklamada lowercase'e normalize edilir.
        var lookup = new Dictionary<string, CampaignLocaleContent>(request.CustomContent!, StringComparer.OrdinalIgnoreCase);
        var normalized = new Dictionary<string, CampaignLocaleContent>();
        foreach (var locale in CampaignLocales.Supported)
        {
            if (!lookup.TryGetValue(locale, out var content)
                || string.IsNullOrWhiteSpace(content.Title)
                || string.IsNullOrWhiteSpace(content.Body))
            {
                throw new AizenBusinessException(
                    $"Custom içerik desteklenen tüm dilleri (tr+en) kapsamalıdır; '{locale}' eksik. Kısmi-locale custom gönderim yasaktır.");
            }
            normalized[locale] = content;
        }

        return (null, JsonSerializer.Serialize(normalized));
    }

    private static string? ValidateAndSerializeRecipients(CreateNotificationCampaignCommand request)
    {
        if (request.TargetMode != CampaignTargetMode.Selected)
            return null;   // All modunda genişletme consumer'da yapılır.

        var ids = request.SelectedRecipientIds?.Where(x => x > 0).Distinct().ToList() ?? new List<long>();
        if (ids.Count == 0)
            throw new AizenBusinessException("TargetMode=Selected için en az bir alıcı id gereklidir.");

        return JsonSerializer.Serialize(ids);
    }
}
