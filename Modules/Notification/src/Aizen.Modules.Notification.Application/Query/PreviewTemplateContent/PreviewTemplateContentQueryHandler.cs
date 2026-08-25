using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Abstraction.Dto;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Services;
using Aizen.Modules.Notification.Domain.Exceptions;
using Aizen.Modules.Notification.Domain.Interface.Service;

namespace Aizen.Modules.Notification.Application.Query.PreviewTemplateContent;

/// <summary>
/// Preview = ÜRETİMLE AYNI ITemplateRenderer. Eksik placeholder / eksik içerik durumları gövdede taşınır (S2S Refit
/// gövde-tipli dönüşte non-2xx'te exception atacağı için 400'ü uç/BFF katmanı üretir — burada 200 + sonuç bayrağı).
/// </summary>
public sealed class PreviewTemplateContentQueryHandler
    : AizenQueryHandler<PreviewTemplateContentQuery, NotificationTemplatePreviewResultDto>
{
    private readonly ITemplateRenderer _renderer;

    public PreviewTemplateContentQueryHandler(ITemplateRenderer renderer) => _renderer = renderer;

    public override async Task<NotificationTemplatePreviewResultDto> Handle(
        PreviewTemplateContentQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var rendered = await _renderer.RenderAsync(
                request.Code, request.Channel, request.Locale, request.Variables, cancellationToken);

            // Sms kanalında segment bilgisini (render edilmiş gövde üzerinden) ekle; diğer kanallarda null kalır.
            var smsSegments = request.Channel == NotificationChannel.Sms
                ? SmsSegmentCalculator.Calculate(rendered.Body)
                : null;

            return new NotificationTemplatePreviewResultDto
            {
                Rendered    = true,
                Title       = rendered.Title,
                Body        = rendered.Body,
                DeepLink    = rendered.DeepLink,
                SmsSegments = smsSegments,
            };
        }
        catch (TemplatePlaceholderMissingException ex)
        {
            // Eksik değişkenler → 400'e çevrilecek (uç katman). Anahtar listesi gövdede.
            return new NotificationTemplatePreviewResultDto
            {
                Rendered    = false,
                MissingKeys = ex.MissingKeys,
            };
        }
        catch (TemplateContentMissingException)
        {
            // (channel, locale) için yayında içerik yok.
            return new NotificationTemplatePreviewResultDto
            {
                Rendered       = false,
                ContentMissing = true,
            };
        }
    }
}
