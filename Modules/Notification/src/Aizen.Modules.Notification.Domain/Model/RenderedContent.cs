namespace Aizen.Modules.Notification.Domain.Model;

/// <summary>
/// Strict renderer'ın kanal-bağımsız çıktısı. Kalıcı NotificationEntity ile birebir eşlenir:
/// Title (InApp/Push başlığı; Email konusu), Body (InApp/Push gövdesi; Email için layout'a sarılmış HTML; Sms metni),
/// DeepLink (varsa render edilmiş derin bağlantı). Preview ile üretim aynı sonucu paylaşsın diye tek tip.
/// </summary>
public sealed record RenderedContent(string Title, string Body, string? DeepLink);
