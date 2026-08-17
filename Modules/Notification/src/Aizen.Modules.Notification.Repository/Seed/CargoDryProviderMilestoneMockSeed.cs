using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Repository.Seed;

/// <summary>
/// Dev/local mock: seeds a few CargoDry provider milestone InApp notifications for provider2 (100011)
/// so the provider notification center can be verified on screen. Idempotent — skips if provider2 already
/// has any milestone notification (Type 306–309). No schema/settlement impact.
/// </summary>
public sealed class CargoDryProviderMilestoneMockSeed
{
    private const long Provider2 = 100011;

    private static readonly NotificationType[] MilestoneTypes =
    {
        NotificationType.CargoDryProviderFirstSale,
        NotificationType.CargoDryProviderMonthlyTargetReached,
        NotificationType.CargoDryProviderTierUp,
        NotificationType.CargoDryProviderStreakMilestone,
    };

    private readonly NotificationDbContext _db;
    private readonly ILogger<CargoDryProviderMilestoneMockSeed> _logger;

    public CargoDryProviderMilestoneMockSeed(NotificationDbContext db, ILogger<CargoDryProviderMilestoneMockSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var already = await _db.Notifications
            .AnyAsync(n => n.RecipientUserId == Provider2 && MilestoneTypes.Contains(n.Type), ct);
        if (already)
        {
            _logger.LogInformation("Milestone mock notifications already present for provider {Pid}; skipping.", Provider2);
            return;
        }

        var items = new[]
        {
            NotificationEntity.Create(Provider2, NotificationType.CargoDryProviderFirstSale,
                NotificationChannel.InApp, "CD_FIRST_SALE_INAPP",
                "İlk satışın gerçekleşti! 🎉",
                "CargoDry komisyon kazancın başladı. İlk satışını tamamladın.",
                "{\"milestoneType\":\"FirstSale\",\"periodKey\":\"ALL\"}"),

            NotificationEntity.Create(Provider2, NotificationType.CargoDryProviderMonthlyTargetReached,
                NotificationChannel.InApp, "CD_MONTHLY_TARGET_INAPP",
                "Aylık hedefini tutturdun! 🎯",
                "Bu ay hedefine ulaştın — 150 USD komisyon barajını geçtin.",
                "{\"milestoneType\":\"MonthlyTargetReached\",\"periodKey\":\"2026-07\",\"displayValue\":\"150 USD\"}"),

            NotificationEntity.Create(Provider2, NotificationType.CargoDryProviderTierUp,
                NotificationChannel.InApp, "CD_TIER_UP_INAPP",
                "Silver kademesine yükseldin! 🏅",
                "Artık her satıştan +%2 daha fazla kazanıyorsun.",
                "{\"milestoneType\":\"TierUp\",\"periodKey\":\"SILVER\",\"displayValue\":\"Silver\"}"),

            NotificationEntity.Create(Provider2, NotificationType.CargoDryProviderStreakMilestone,
                NotificationChannel.InApp, "CD_STREAK_INAPP",
                "3 ay üst üste satış! 🔥",
                "Serini sürdür — momentum sende.",
                "{\"milestoneType\":\"StreakMilestone\",\"periodKey\":\"3\",\"displayValue\":\"3\"}"),
        };

        foreach (var n in items) n.MarkAsSent();

        await _db.Notifications.AddRangeAsync(items, ct);
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Seeded {Count} milestone mock notifications for provider {Pid}.", items.Length, Provider2);
    }
}
