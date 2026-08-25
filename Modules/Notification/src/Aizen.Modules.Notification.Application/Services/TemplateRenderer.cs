using System.Text.Encodings.Web;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Domain.Exceptions;
using Aizen.Modules.Notification.Domain.Interface.Repository;
using Aizen.Modules.Notification.Domain.Interface.Service;
using Aizen.Modules.Notification.Domain.Model;
using Microsoft.Extensions.Options;

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
    private readonly IReadOnlyCollection<string>            _allowedDeepLinkHosts;

    // deepLinkOptions opsiyonel: verilmezse allowlist boş (yalnız göreli yollara izin). DI IOptions'ı enjekte eder.
    public TemplateRenderer(
        INotificationTemplateContentRepository contentRepo,
        IEmailLayoutRepository layoutRepo,
        ITemplateInterpolator interpolator,
        IOptions<DeepLinkOptions>? deepLinkOptions = null)
    {
        _contentRepo  = contentRepo;
        _layoutRepo   = layoutRepo;
        _interpolator = interpolator;
        _allowedDeepLinkHosts = deepLinkOptions?.Value?.AllowedHosts ?? new List<string>();
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

        // Eksik değişkenleri tüm alanlar boyunca topla, sonra tek net hata ver. `vars` parametresi ile aynı fonksiyon
        // hem ham hem HTML-escape'li değer sözlüğüyle çağrılabilir (email gövdesi için escape'li).
        var missing = new List<string>();
        string RenderStrict(string? tpl, IReadOnlyDictionary<string, string> vars)
        {
            if (string.IsNullOrEmpty(tpl)) return string.Empty;
            var rendered = _interpolator.TryInterpolateStrict(tpl, vars, out var miss);
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
            // XSS: Email gövdesi HTML'dir → değişken DEĞERLERİ interpolasyondan ÖNCE HTML-encode edilir (şablon
            // markup'ı DEĞİL). Konu düz metindir → ham değerlerle render edilir. Eksik-placeholder davranışı değişmez
            // (anahtar kümesi aynı). DeepLink de ham değerlerle (aşağıda ayrıca scheme/host doğrulaması yapılır).
            var htmlEncodedVars = ToHtmlEncoded(variables);

            var subject   = RenderStrict(content.SubjectTemplate ?? content.TitleTemplate, variables);
            var innerHtml = RenderStrict(content.HtmlTemplate ?? content.BodyTemplate, htmlEncodedVars);

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
            body  = RenderStrict(content.SmsTextTemplate ?? content.BodyTemplate, variables);   // düz metin → escape yok
        }
        else // InApp / Push (düz metin → escape yok)
        {
            title = RenderStrict(content.TitleTemplate, variables);
            body  = RenderStrict(content.BodyTemplate, variables);
        }

        var deepLink = string.IsNullOrEmpty(content.DeepLinkTemplate)
            ? null
            : RenderStrict(content.DeepLinkTemplate, variables);

        if (missing.Count > 0)
            throw new TemplatePlaceholderMissingException(templateCode, missing);

        // Güvenlik: render edilen derin bağlantı yalnız göreli yol ya da izinli http/https host olabilir (javascript:/
        // data:/dış host/protokol-göreli reddedilir). Render tam başarılıysa doğrulanır.
        if (!string.IsNullOrEmpty(deepLink))
            DeepLinkValidator.Validate(deepLink, _allowedDeepLinkHosts, templateCode);

        return new RenderedContent(title, body, string.IsNullOrEmpty(deepLink) ? null : deepLink);
    }

    // Değişken DEĞERLERİNİ HTML-encode eden bir kopya sözlük (anahtarlar aynı kalır → strict eksik-kontrolü değişmez).
    private static Dictionary<string, string> ToHtmlEncoded(IReadOnlyDictionary<string, string> variables)
    {
        var encoded = new Dictionary<string, string>(variables.Count);
        foreach (var kv in variables)
            encoded[kv.Key] = HtmlEncoder.Default.Encode(kv.Value ?? string.Empty);
        return encoded;
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
