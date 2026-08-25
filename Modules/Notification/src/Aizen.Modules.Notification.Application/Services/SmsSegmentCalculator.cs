using Aizen.Modules.Notification.Abstraction.Dto;

namespace Aizen.Modules.Notification.Application.Services;

/// <summary>
/// SMS segment hesabı (SAF). GSM-7 varsayılan alfabesi (+ extension tablosu, her extension char = 2 septet) dışında
/// bir karakter varsa kodlama UCS-2'ye düşer (Türkçe ş/ğ/ı GSM-7'de yoktur → UCS-2). Sınırlar: GSM-7 160/153,
/// UCS-2 70/67. UCS-2 uzunluğu UTF-16 birimi sayılır (astral emoji = 2 birim).
/// </summary>
public static class SmsSegmentCalculator
{
    // 3GPP TS 23.038 GSM-7 varsayılan alfabesi (ESC hariç; \n ve \r dahil).
    private const string Gsm7Basic =
        "@£$¥èéùìòÇ\nØø\rÅåΔ_ΦΓΛΩΠΨΣΘΞÆæßÉ !\"#¤%&'()*+,-./0123456789:;<=>?" +
        "¡ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÑÜ§¿abcdefghijklmnopqrstuvwxyzäöñüà";

    // Extension tablosu: her biri ESC + char olarak 2 septet yer kaplar.
    private const string Gsm7Extension = "\f^{}\\[~]|€";

    private static readonly HashSet<char> BasicSet     = new(Gsm7Basic);
    private static readonly HashSet<char> ExtensionSet = new(Gsm7Extension);

    public static NotificationTemplateSmsSegmentsDto Calculate(string? text)
    {
        text ??= string.Empty;

        var isGsm = true;
        var septets = 0;
        foreach (var ch in text)
        {
            if (BasicSet.Contains(ch)) { septets += 1; }
            else if (ExtensionSet.Contains(ch)) { septets += 2; }
            else { isGsm = false; break; }
        }

        if (isGsm)
        {
            var segments = septets <= 160 ? 1 : CeilDiv(septets, 153);
            return new NotificationTemplateSmsSegmentsDto
            {
                Encoding = "GSM-7",
                Length   = septets,
                Segments = segments,
            };
        }

        // UCS-2: UTF-16 kod birimi sayılır (astral karakter = 2 birim).
        var units = text.Length;
        var ucsSegments = units <= 70 ? 1 : CeilDiv(units, 67);
        return new NotificationTemplateSmsSegmentsDto
        {
            Encoding = "UCS-2",
            Length   = units,
            Segments = ucsSegments,
        };
    }

    private static int CeilDiv(int a, int b) => (a + b - 1) / b;
}
