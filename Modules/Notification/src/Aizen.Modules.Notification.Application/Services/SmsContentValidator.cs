using Aizen.Core.Infrastructure.Exception;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// SMS içerik doğrulaması (SAF). SMS metninde '&lt;' HER ZAMAN hatadır — HTML SMS'e girmez. Hem taslak-kaydetmede hem
/// render'da aynı kural uygulanır; ihlal olan şablonu adıyla belirten net hata fırlatır.
/// </summary>
public static class SmsContentValidator
{
    public static void EnsureNoHtml(string? smsTemplateText, string templateCode)
    {
        if (smsTemplateText is not null && smsTemplateText.Contains('<'))
            throw new AizenBusinessException(
                $"SMS şablonu '{templateCode}' HTML içeremez ('<' bulundu). SMS düz metin olmalıdır.");
    }
}
