using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Repository.Seed;

/// <summary>
/// Wave 4A — CargoDry supply-order + stock-request notification templates, seeded with genuine TR + EN content and a
/// DeepLink on every template (rendered from the per-audience <c>{{deepLink}}</c> variable the consumers pass;
/// owner emails additionally carry an https fallback line via <c>{{webFallbackUrl}}</c>). INSERT-ONLY / idempotent
/// (guards on template code + per-(template,channel,locale) content), matching the main seeder's contract.
///
/// Runs BEFORE the main seeder's generic content step so that step's single-en-locale, DeepLink-less default is
/// skipped for these codes (its guard sees content already exists for the channel).
/// </summary>
public static class CargoDryWave4ANotificationSeed
{
    private const string DeepLinkVar = "{{deepLink}}";

    // (code, type, channel, trTitle, trBody, enTitle, enBody)
    private static readonly (string Code, NotificationType Type, NotificationChannel Channel,
        string TrTitle, string TrBody, string EnTitle, string EnBody)[] Templates =
    {
        // ── Supply order — owner ─────────────────────────────────────────────────
        ("CD_SUPPLY_ASSIGNED_OWNER_INAPP", NotificationType.CargoDrySupplyOrderAssignedOwner, NotificationChannel.InApp,
            "Servis sağlayıcı atandı", "CargoDry talebiniz {{requestCode}} için bir servis sağlayıcı atandı.",
            "Provider assigned", "A service provider has been assigned to your CargoDry order {{requestCode}}."),
        ("CD_SUPPLY_ASSIGNED_OWNER_EMAIL", NotificationType.CargoDrySupplyOrderAssignedOwner, NotificationChannel.Email,
            "CargoDry siparişinize sağlayıcı atandı", "Merhaba,\n\nCargoDry talebiniz {{requestCode}} için bir servis sağlayıcı atandı.\n\nInktavia Marine",
            "A provider was assigned to your CargoDry order", "Hello,\n\nA service provider has been assigned to your CargoDry order {{requestCode}}.\n\nInktavia Marine"),

        ("CD_SUPPLY_DELIVERED_INAPP", NotificationType.CargoDrySupplyOrderDelivered, NotificationChannel.InApp,
            "Kitiniz teslim edildi", "CargoDry siparişiniz {{requestCode}} teslim edildi.",
            "Your kit was delivered", "Your CargoDry order {{requestCode}} has been delivered."),
        ("CD_SUPPLY_DELIVERED_EMAIL", NotificationType.CargoDrySupplyOrderDelivered, NotificationChannel.Email,
            "CargoDry siparişiniz teslim edildi", "Merhaba,\n\nCargoDry siparişiniz {{requestCode}} teslim edildi.\n\nInktavia Marine",
            "Your CargoDry order was delivered", "Hello,\n\nYour CargoDry order {{requestCode}} has been delivered.\n\nInktavia Marine"),

        ("CD_SUPPLY_SHIPPED_INAPP", NotificationType.CargoDrySupplyOrderShipped, NotificationChannel.InApp,
            "Siparişiniz kargolandı", "CargoDry siparişiniz {{requestCode}} kargoya verildi. Takip kodu: {{trackingCode}}.",
            "Your order shipped", "Your CargoDry order {{requestCode}} has shipped. Tracking code: {{trackingCode}}."),
        ("CD_SUPPLY_SHIPPED_EMAIL", NotificationType.CargoDrySupplyOrderShipped, NotificationChannel.Email,
            "CargoDry siparişiniz kargolandı", "Merhaba,\n\nCargoDry siparişiniz {{requestCode}} kargoya verildi.\nTakip kodu: {{trackingCode}}\n\nInktavia Marine",
            "Your CargoDry order shipped", "Hello,\n\nYour CargoDry order {{requestCode}} has shipped.\nTracking code: {{trackingCode}}\n\nInktavia Marine"),

        ("CD_SUPPLY_COMPLETED_INAPP", NotificationType.CargoDrySupplyOrderCompleted, NotificationChannel.InApp,
            "Siparişiniz tamamlandı", "CargoDry siparişiniz {{requestCode}} tamamlandı.",
            "Your order is complete", "Your CargoDry order {{requestCode}} is complete."),
        ("CD_SUPPLY_COMPLETED_EMAIL", NotificationType.CargoDrySupplyOrderCompleted, NotificationChannel.Email,
            "CargoDry siparişiniz tamamlandı", "Merhaba,\n\nCargoDry siparişiniz {{requestCode}} tamamlandı.\n\nInktavia Marine",
            "Your CargoDry order is complete", "Hello,\n\nYour CargoDry order {{requestCode}} is complete.\n\nInktavia Marine"),

        ("CD_SUPPLY_CANCELLED_OWNER_INAPP", NotificationType.CargoDrySupplyOrderCancelledOwner, NotificationChannel.InApp,
            "Siparişiniz iptal edildi", "CargoDry siparişiniz {{requestCode}} iptal edildi.",
            "Your order was cancelled", "Your CargoDry order {{requestCode}} has been cancelled."),
        ("CD_SUPPLY_CANCELLED_OWNER_EMAIL", NotificationType.CargoDrySupplyOrderCancelledOwner, NotificationChannel.Email,
            "CargoDry siparişiniz iptal edildi", "Merhaba,\n\nCargoDry siparişiniz {{requestCode}} iptal edildi.\n\nInktavia Marine",
            "Your CargoDry order was cancelled", "Hello,\n\nYour CargoDry order {{requestCode}} has been cancelled.\n\nInktavia Marine"),

        ("CD_SUPPLY_AWAITING_OWNER_INAPP", NotificationType.CargoDrySupplyOrderAwaitingShipmentOwner, NotificationChannel.InApp,
            "Siparişiniz kargoya hazırlanıyor", "CargoDry siparişiniz {{requestCode}} kargoya hazırlanıyor.",
            "Your order is being prepared for shipping", "Your CargoDry order {{requestCode}} is being prepared for shipping."),
        ("CD_SUPPLY_AWAITING_OWNER_EMAIL", NotificationType.CargoDrySupplyOrderAwaitingShipmentOwner, NotificationChannel.Email,
            "CargoDry siparişiniz kargoya hazırlanıyor", "Merhaba,\n\nCargoDry siparişiniz {{requestCode}} kargoya hazırlanıyor.\n\nInktavia Marine",
            "Your CargoDry order is being prepared for shipping", "Hello,\n\nYour CargoDry order {{requestCode}} is being prepared for shipping.\n\nInktavia Marine"),

        // ── Supply order — provider ──────────────────────────────────────────────
        ("CD_SUPPLY_CANCELLED_PROVIDER_INAPP", NotificationType.CargoDrySupplyOrderCancelledProvider, NotificationChannel.InApp,
            "Atanan sipariş iptal edildi", "Size atanan CargoDry siparişi {{requestCode}} iptal edildi.",
            "Assigned order cancelled", "The CargoDry order {{requestCode}} assigned to you has been cancelled."),
        ("CD_SUPPLY_CANCELLED_PROVIDER_EMAIL", NotificationType.CargoDrySupplyOrderCancelledProvider, NotificationChannel.Email,
            "Atanan CargoDry siparişi iptal edildi", "Merhaba,\n\nSize atanan CargoDry siparişi {{requestCode}} iptal edildi.\n\nDetay: {{deepLink}}\n\nInktavia Marine",
            "Assigned CargoDry order cancelled", "Hello,\n\nThe CargoDry order {{requestCode}} assigned to you has been cancelled.\n\nDetails: {{deepLink}}\n\nInktavia Marine"),

        // ── Supply order — admin activity feed (generic; InApp only) ─────────────
        ("CD_SUPPLY_ADMIN_ACTIVITY_INAPP", NotificationType.CargoDrySupplyOrderAdminActivity, NotificationChannel.InApp,
            "CargoDry sipariş etkinliği", "{{requestCode}} — {{eventLabel}}",
            "CargoDry order activity", "{{requestCode}} — {{eventLabel}}"),

        // ── Stock request — admin (created) ──────────────────────────────────────
        ("CD_STOCK_CREATED_ADMIN_INAPP", NotificationType.CargoDryStockRequestCreatedAdmin, NotificationChannel.InApp,
            "Yeni stok talebi", "Bir sağlayıcı {{productCode}} ürünü için stok talebi oluşturdu.",
            "New stock request", "A provider submitted a stock request for {{productCode}}."),
        ("CD_STOCK_CREATED_ADMIN_EMAIL", NotificationType.CargoDryStockRequestCreatedAdmin, NotificationChannel.Email,
            "Yeni CargoDry stok talebi", "Merhaba,\n\nBir sağlayıcı {{productCode}} ürünü için stok talebi oluşturdu.\n\nYönetim panelinde aç: {{deepLink}}\n\nInktavia Marine",
            "New CargoDry stock request", "Hello,\n\nA provider submitted a stock request for {{productCode}}.\n\nOpen in admin panel: {{deepLink}}\n\nInktavia Marine"),

        // ── Stock request — provider ─────────────────────────────────────────────
        ("CD_STOCK_APPROVED_INAPP", NotificationType.CargoDryStockRequestApproved, NotificationChannel.InApp,
            "Stok talebiniz onaylandı", "{{productCode}} için stok talebiniz onaylandı.",
            "Stock request approved", "Your stock request for {{productCode}} was approved."),
        ("CD_STOCK_APPROVED_EMAIL", NotificationType.CargoDryStockRequestApproved, NotificationChannel.Email,
            "CargoDry stok talebiniz onaylandı", "Merhaba,\n\n{{productCode}} için stok talebiniz onaylandı.\n\nDetay: {{deepLink}}\n\nInktavia Marine",
            "Your CargoDry stock request was approved", "Hello,\n\nYour stock request for {{productCode}} was approved.\n\nDetails: {{deepLink}}\n\nInktavia Marine"),

        ("CD_STOCK_SHIPPED_INAPP", NotificationType.CargoDryStockRequestShipped, NotificationChannel.InApp,
            "Stok talebiniz kargolandı", "{{productCode}} için stok talebiniz kargolandı. Takip kodu: {{trackingCode}}.",
            "Stock request shipped", "Your stock request for {{productCode}} has shipped. Tracking code: {{trackingCode}}."),
        ("CD_STOCK_SHIPPED_EMAIL", NotificationType.CargoDryStockRequestShipped, NotificationChannel.Email,
            "CargoDry stok talebiniz kargolandı", "Merhaba,\n\n{{productCode}} için stok talebiniz kargolandı.\nTakip kodu: {{trackingCode}}\n\nDetay: {{deepLink}}\n\nInktavia Marine",
            "Your CargoDry stock request shipped", "Hello,\n\nYour stock request for {{productCode}} has shipped.\nTracking code: {{trackingCode}}\n\nDetails: {{deepLink}}\n\nInktavia Marine"),

        ("CD_STOCK_REJECTED_INAPP", NotificationType.CargoDryStockRequestRejected, NotificationChannel.InApp,
            "Stok talebiniz reddedildi", "{{productCode}} için stok talebiniz reddedildi. Sebep: {{reason}}.",
            "Stock request rejected", "Your stock request for {{productCode}} was rejected. Reason: {{reason}}."),
        ("CD_STOCK_REJECTED_EMAIL", NotificationType.CargoDryStockRequestRejected, NotificationChannel.Email,
            "CargoDry stok talebiniz reddedildi", "Merhaba,\n\n{{productCode}} için stok talebiniz reddedildi.\nSebep: {{reason}}\n\nDetay: {{deepLink}}\n\nInktavia Marine",
            "Your CargoDry stock request was rejected", "Hello,\n\nYour stock request for {{productCode}} was rejected.\nReason: {{reason}}\n\nDetails: {{deepLink}}\n\nInktavia Marine"),

        // ── Stock request — admin activity feed (generic; InApp only) ────────────
        ("CD_STOCK_ADMIN_ACTIVITY_INAPP", NotificationType.CargoDryStockRequestAdminActivity, NotificationChannel.InApp,
            "Stok talebi etkinliği", "{{productCode}} — {{eventLabel}}",
            "Stock request activity", "{{productCode}} — {{eventLabel}}"),
    };

    public static async Task SeedAsync(NotificationDbContext db, CancellationToken ct = default)
    {
        foreach (var t in Templates)
        {
            var template = await db.NotificationTemplates.FirstOrDefaultAsync(x => x.TemplateCode == t.Code, ct);
            if (template is null)
            {
                template = NotificationTemplateEntity.Create(
                    t.Code, t.Code, t.Type, t.Channel, t.TrTitle, t.TrBody);
                await db.NotificationTemplates.AddAsync(template, ct);
                await db.SaveChangesAsync(ct); // need the generated Id for content rows
            }

            await UpsertContentAsync(db, template.Id, t.Channel, "tr", t.TrTitle, t.TrBody, ct);
            await UpsertContentAsync(db, template.Id, t.Channel, "en", t.EnTitle, t.EnBody, ct);
        }
        await db.SaveChangesAsync(ct);
    }

    private static async Task UpsertContentAsync(
        NotificationDbContext db, long templateId, NotificationChannel channel,
        string locale, string title, string body, CancellationToken ct)
    {
        var exists = await db.NotificationTemplateContents
            .AnyAsync(c => c.TemplateId == templateId && c.Channel == channel && c.Locale == locale, ct);
        if (exists) return;

        var isEmail = channel == NotificationChannel.Email;
        await db.NotificationTemplateContents.AddAsync(NotificationTemplateContentEntity.Create(
            templateId:      templateId,
            channel:         channel,
            locale:          locale,
            version:         1,
            status:          TemplateContentStatus.Published,
            titleTemplate:   title,
            bodyTemplate:    body,
            subjectTemplate: isEmail ? title : null,
            htmlTemplate:    isEmail ? body : null,
            deepLinkTemplate: DeepLinkVar,
            layoutCode:      isEmail ? DefaultTemplateContent.DefaultLayoutCode : null), ct);
    }
}
