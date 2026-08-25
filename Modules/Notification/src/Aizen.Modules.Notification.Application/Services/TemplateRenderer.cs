using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Exceptions;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Aizen.Modules.Notification.Domain.Model;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// Üretim ve (ileride) preview'in paylaştığı TEK strict renderer. Bkz. <see cref="ITemplateRenderer"/>.
/// </summary>
public sealed class TemplateRenderer : ITemplateRenderer
{
    private const string DefaultLocale     = "en";
    private const string DefaultLayoutCode = "DEFAULT";

    private readonly INotificationTemplateContentRepository _contentRepo;
    private readonly IEmailLayoutRepository                 _layoutRepo;
    private readonly ITemplateInterpolator                  _interpolator;

    public TemplateRenderer(
        INotificationTemplateContentRepository contentRepo,
        IEmailLayoutRepository layoutRepo,
        ITemplateInterpolator interpolator)
    {
        _contentRepo  = contentRepo;
        _layoutRepo   = layoutRepo;
        _interpolator = interpolator;
    }

    public async Task<RenderedContent> RenderAsync(
        string templateCode,
        NotificationChannel channel,
        string locale,
        IReadOnlyDictionary<string, string> variables,
        CancellationToken ct = default)
    {
        var normalizedLocale = string.IsNullOrWhiteSpace(locale) ? DefaultLocale : locale.ToLowerInvariant();

        var published = await _contentRepo.GetPublishedByTemplateCodeAndChannelAsync(templateCode, channel, ct);
        if (published.Count == 0)
            throw new TemplateContentMissingException(templateCode, channel, normalizedLocale);

        // Fallback zinciri: istenen locale → 'en' → ilk Published. Bir locale içinde en yüksek Version seçilir.
        var content = Select(published, normalizedLocale)
                      ?? Select(published, DefaultLocale)
                      ?? published.OrderByDescending(c => c.Version).ThenByDescending(c => c.Id).First();

        // Eksik değişkenleri tüm alanlar boyunca topla, sonra tek net hata ver.
        var missing = new List<string>();
        string RenderStrict(string? tpl)
        {
            if (string.IsNullOrEmpty(tpl)) return string.Empty;
            var rendered = _interpolator.TryInterpolateStrict(tpl, variables, out var miss);
            if (rendered is null)
            {
                foreach (var k in miss)
                    if (!missing.Contains(k)) missing.Add(k);
                return string.Empty;
            }
            return rendered;
        }

        string title;
        string body;

        if (channel == NotificationChannel.Email)
        {
            // Email: konu = SubjectTemplate; gövde = HtmlTemplate'in layout kabuğuna sarılmış hâli.
            var subject   = RenderStrict(content.SubjectTemplate ?? content.TitleTemplate);
            var innerHtml = RenderStrict(content.HtmlTemplate ?? content.BodyTemplate);

            var layoutCode = string.IsNullOrWhiteSpace(content.LayoutCode) ? DefaultLayoutCode : content.LayoutCode!;
            var layout = await _layoutRepo.GetActiveByCodeAsync(layoutCode, ct);

            title = subject;
            body  = layout is not null
                ? layout.HtmlShell.Replace(EmailLayoutEntity.ContentPlaceholder, innerHtml, StringComparison.Ordinal)
                : innerHtml; // layout bulunamazsa sarma yapılmadan gövde kullanılır (güvenli geri düşüş)
        }
        else if (channel == NotificationChannel.Sms)
        {
            // Render-time savunma: SMS metni HTML içeremez ('<'). Taslak-kaydetme bunu zaten engeller; bu ek kat.
            SmsContentValidator.EnsureNoHtml(content.SmsTextTemplate ?? content.BodyTemplate, templateCode);
            title = string.Empty;
            body  = RenderStrict(content.SmsTextTemplate ?? content.BodyTemplate);
        }
        else // InApp / Push
        {
            title = RenderStrict(content.TitleTemplate);
            body  = RenderStrict(content.BodyTemplate);
        }

        var deepLink = string.IsNullOrEmpty(content.DeepLinkTemplate) ? null : RenderStrict(content.DeepLinkTemplate);

        if (missing.Count > 0)
            throw new TemplatePlaceholderMissingException(templateCode, missing);

        return new RenderedContent(title, body, string.IsNullOrEmpty(deepLink) ? null : deepLink);
    }

    // Bir locale için en yüksek Version'lı Published içeriği seçer (yoksa null).
    private static NotificationTemplateContentEntity? Select(
        List<NotificationTemplateContentEntity> published, string locale)
        => published
            .Where(c => string.Equals(c.Locale, locale, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(c => c.Version)
            .ThenByDescending(c => c.Id)
            .FirstOrDefault();
}
