namespace Aizen.Modules.Notification.Abstraction.Dto;

/// <summary>
/// Preview sonucu. Rendered=true ise Title/Body/DeepLink dolu. Eksik placeholder varsa Rendered=false ve MissingKeys
/// dolu (uç/BFF katmanı bunu 400'e çevirir — modül S2S Refit gövde-tipli dönüşte non-2xx'te exception atacağı için
/// sonucu gövdede taşır). ContentMissing=true ise (channel,locale) için yayında içerik yok.
/// </summary>
public sealed class NotificationTemplatePreviewResultDto
{
    public bool                 Rendered       { get; init; }
    public string?              Title          { get; init; }
    public string?              Body           { get; init; }
    public string?              DeepLink       { get; init; }
    public IReadOnlyList<string> MissingKeys   { get; init; } = new List<string>();
    public bool                 ContentMissing { get; init; }
}
