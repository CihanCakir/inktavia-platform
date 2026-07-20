using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Repository.Seed;

/// <summary>
/// Dev/local mock: seeds ~24 mixed InApp notifications (8 unread + 16 read) for provider2 (100011)
/// to exercise infinite-scroll pagination in the notification dropdown. Idempotent — guarded by
/// TemplateCode "SEED_DISPLAY". Development-gated.
/// </summary>
public sealed class NotificationDisplayMockSeed
{
    private const long Provider2 = 100011;
    private const string SeedMarker = "SEED_DISPLAY";

    private static readonly (NotificationType Type, string Title, string Body)[] Templates =
    [
        (NotificationType.CargoDryKitActivated,       "Kit aktive edildi",                     "CargoDry kitiniz aktif edildi ve kullanıma hazır."),
        (NotificationType.CargoDryKitExpiringReminder,  "Kit süresi dolmak üzere",              "Kitinizin süresi yakında doluyor, lütfen yenileme işlemini başlatın."),
        (NotificationType.CargoDryKitRenewed,          "Kit yenilendi",                        "CargoDry kitiniz başarıyla yenilendi."),
        (NotificationType.CargoDryKitRevoked,          "Kit iptal edildi",                     "CargoDry kitiniz iptal edildi. Detaylar için destek ile iletişime geçin."),
        (NotificationType.ServiceRequestCreated,       "Yeni servis talebi oluşturuldu",       "Bir müşteri yeni bir servis talebi oluşturdu."),
        (NotificationType.OfferCreated,                  "Yeni teklif alındı",                   "Servis talebinize yeni bir teklif geldi."),
        (NotificationType.ServiceRequestStatusChanged, "Talep durumu güncellendi",              "Servis talebinizin durumu güncellendi."),
        (NotificationType.AssignmentCreated,             "Talep atandı",                          "Bir servis talebi size atandı."),
        (NotificationType.PaymentCaptured,             "Ödeme alındı",                          "Ödemeniz başarıyla gerçekleştirildi."),
        (NotificationType.PayoutCompleted,             "Ödeme tamamlandı",                      "Komisyon ödemeniz hesabınıza aktarıldı."),
        (NotificationType.CargoDryProviderFirstSale,   "İlk satışınız gerçekleşti",            "Tebrikler! İlk CargoDry satışınızı tamamladınız."),
        (NotificationType.CargoDryProviderTierUp,      "Kademe yükseltmesi",                   "CargoDry satış kademeniz yükseltildi."),
    ];

    private readonly NotificationDbContext _db;
    private readonly ILogger<NotificationDisplayMockSeed> _logger;

    public NotificationDisplayMockSeed(NotificationDbContext db, ILogger<NotificationDisplayMockSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var already = await _db.Notifications
            .AnyAsync(n => n.RecipientUserId == Provider2 && n.TemplateCode == SeedMarker, ct);
        if (already)
        {
            _logger.LogInformation("Display mock notifications already present for provider {Pid}; skipping.", Provider2);
            return;
        }

        var now = DateTimeOffset.UtcNow;
        const int total = 24;
        const int unreadCount = 8;
        var items = new NotificationEntity[total];

        for (var i = 0; i < total; i++)
        {
            var t = Templates[i % Templates.Length];
            var createdAt = now.AddHours(-i * 6);
            DateTimeOffset? readAt = i < unreadCount ? null : createdAt.AddMinutes(30);

            items[i] = NotificationEntity.CreateSeed(
                Provider2,
                t.Type,
                NotificationChannel.InApp,
                SeedMarker,
                t.Title,
                t.Body,
                null,
                createdAt,
                readAt);
        }

        await _db.Notifications.AddRangeAsync(items, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded {Count} display mock notifications for provider {Pid}.", total, Provider2);
    }
}
