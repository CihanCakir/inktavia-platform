namespace Aizen.Modules.Notification.Abstraction.Enum;

public enum NotificationStatus
{
    Pending   = 0,
    Sent      = 1,
    Delivered = 2,
    Failed    = 3,
    Read      = 4,
    // FAZ15 (#34): gönderim denemesi BAŞLADI ama sonucu (Sent/Failed) henüz yazılmadı. Değişmez: Pending satırı =
    // e-posta HİÇ gönderilmedi. Transport dönüp sonuç yazılamadan çökme olursa satır 'Sending' kalır (denendi, sonuç
    // belirsiz) — asla yanıltıcı Pending değil. int kolonu olduğundan yeni değer migration gerektirmez.
    Sending   = 5,
}
