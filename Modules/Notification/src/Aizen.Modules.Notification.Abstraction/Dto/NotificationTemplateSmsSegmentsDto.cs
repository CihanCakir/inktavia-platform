namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>
/// SMS segment bilgisi (yalnız Sms kanalı preview'inde dolar). Encoding = "GSM-7" | "UCS-2"; Length = kodlamaya göre
/// uzunluk (GSM-7 septet, extension char = 2; UCS-2 UTF-16 birimi); Segments = concatenated-SMS parça sayısı.
/// </summary>
public sealed class NotificationTemplateSmsSegmentsDto
{
    public string Encoding { get; init; } = default!;
    public int    Length   { get; init; }
    public int    Segments { get; init; }
}
