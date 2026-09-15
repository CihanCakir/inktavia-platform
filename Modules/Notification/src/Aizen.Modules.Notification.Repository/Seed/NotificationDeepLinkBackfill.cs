using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Repository.Seed;

/// <summary>
/// Wave 4A — retrofit a DeepLink onto EXISTING SR/CargoDry templates (the original complaint: "no mail ever contained
/// a link"). Idempotent: sets <c>DeepLinkTemplate</c> only on content rows where it is currently NULL (never overrides
/// an admin edit or the Wave 4A templates, which already carry one). Relative paths only — always valid for the
/// validator and for in-app navigation; the email dispatcher absolutizes them for the email channel.
///
/// SAFETY: each DeepLinkTemplate uses ONLY a variable the emitting consumer is guaranteed to pass (audited) — the
/// strict renderer throws on a missing placeholder. Templates whose consumer passes no usable id/code get a STATIC
/// list path (no placeholder) so they still gain a link without any risk.
/// </summary>
public static class NotificationDeepLinkBackfill
{
    // templateCode → DeepLinkTemplate (relative). Only guaranteed-present variables are referenced.
    private static readonly Dictionary<string, string> Map = new(StringComparer.Ordinal)
    {
        // ── Owner-facing SR — consumer passes serviceRequestId → deep link to the request ──
        ["SR_OFFER_RECEIVED_INAPP"]                        = "/service-requests/{{serviceRequestId}}",
        ["SR_OFFER_RECEIVED_EMAIL"]                        = "/service-requests/{{serviceRequestId}}",
        ["SR_JOB_STARTED_INAPP"]                           = "/service-requests/{{serviceRequestId}}",
        ["SR_JOB_STARTED_EMAIL"]                           = "/service-requests/{{serviceRequestId}}",
        ["SR_TRIP_STARTED_INAPP"]                          = "/service-requests/{{serviceRequestId}}",
        ["SR_TRIP_STARTED_EMAIL"]                          = "/service-requests/{{serviceRequestId}}",
        ["SR_COMPLETION_SUBMITTED_INAPP"]                  = "/service-requests/{{serviceRequestId}}",
        ["SR_COMPLETION_SUBMITTED_EMAIL"]                  = "/service-requests/{{serviceRequestId}}",
        ["SR_COMPLETION_AUTOAPPROVE_APPROACHING_INAPP"]    = "/service-requests/{{serviceRequestId}}",
        ["SR_COMPLETION_AUTOAPPROVE_APPROACHING_EMAIL"]    = "/service-requests/{{serviceRequestId}}",
        ["SR_DISPUTE_OPENED_INAPP"]                        = "/service-requests/{{serviceRequestId}}",
        ["SR_DISPUTE_RESOLVED_INAPP"]                      = "/service-requests/{{serviceRequestId}}",

        // ── Provider-facing SR — consumer passes serviceRequestId → provider portal path ──
        ["SR_OFFER_CREATED_INAPP"]                         = "/app/service-requests/{{serviceRequestId}}",
        ["SR_OFFER_CREATED_EMAIL"]                         = "/app/service-requests/{{serviceRequestId}}",
        ["SR_OFFER_ACCEPTED_INAPP"]                        = "/app/service-requests/{{serviceRequestId}}",
        ["SR_OFFER_ACCEPTED_EMAIL"]                        = "/app/service-requests/{{serviceRequestId}}",
        ["SR_ASSIGNMENT_CREATED_INAPP"]                    = "/app/service-requests/{{serviceRequestId}}",
        ["SR_ASSIGNMENT_CREATED_EMAIL"]                    = "/app/service-requests/{{serviceRequestId}}",
        ["SR_COMPLETION_APPROVED_INAPP"]                   = "/app/service-requests/{{serviceRequestId}}",
        ["SR_COMPLETION_APPROVED_EMAIL"]                   = "/app/service-requests/{{serviceRequestId}}",

        // ── SR without a usable id/code in scope → static list path (no placeholder, always safe) ──
        ["SR_CREATED_INAPP"]                               = "/service-requests",
        ["SR_PUBLISHED_INAPP"]                             = "/service-requests",
        ["SR_PUBLISHED_EMAIL"]                             = "/service-requests",
        ["SR_STATUS_CHANGED_INAPP"]                        = "/service-requests",
        ["SR_OFFER_REJECTED_INAPP"]                        = "/service-requests",
        ["SR_COMPLETION_REJECTED_INAPP"]                   = "/service-requests",
        ["SR_PAYMENT_RELEASED_INAPP"]                      = "/service-requests",
        ["SR_AREA_OPPORTUNITY_INAPP"]                      = "/app/service-requests",
        ["SR_AREA_OPPORTUNITY_EMAIL"]                      = "/app/service-requests",
        ["SR_MAINTENANCE_REMINDER_DUE_INAPP"]              = "/maintenance",
        ["SR_MAINTENANCE_REMINDER_DUE_EMAIL"]              = "/maintenance",

        // ── CargoDry kit — consumer passes kitCode (NOT kitId) → deep link by code ──
        ["CD_KIT_ACTIVATED_INAPP"]                         = "/cargodry/kits/{{kitCode}}",
        ["CD_KIT_EXPIRED_INAPP"]                           = "/cargodry/kits/{{kitCode}}",
        ["CD_KIT_EXPIRING_INAPP"]                          = "/cargodry/kits/{{kitCode}}",
        ["CD_KIT_RENEWED_INAPP"]                           = "/cargodry/kits/{{kitCode}}",
        ["CD_KIT_REVOKED_INAPP"]                           = "/cargodry/kits/{{kitCode}}",
        ["CD_RENEWAL_NOTIFICATION_INAPP"]                  = "/cargodry/kits/{{kitCode}}",
        ["CD_RENEWAL_NOTIFICATION_EMAIL"]                  = "/cargodry/kits/{{kitCode}}",

        // ── CargoDry provider milestones — no kit/id in scope → provider dashboard (static) ──
        ["CD_FIRST_SALE_INAPP"]                            = "/app/cargodry",
        ["CD_MONTHLY_TARGET_INAPP"]                        = "/app/cargodry",
        ["CD_TIER_UP_INAPP"]                               = "/app/cargodry",
        ["CD_STREAK_INAPP"]                                = "/app/cargodry",
    };

    public static async Task SeedAsync(NotificationDbContext db, CancellationToken ct = default)
    {
        foreach (var (code, deepLink) in Map)
        {
            var template = await db.NotificationTemplates.FirstOrDefaultAsync(x => x.TemplateCode == code, ct);
            if (template is null) continue;

            var contents = await db.NotificationTemplateContents
                .Where(c => c.TemplateId == template.Id && c.DeepLinkTemplate == null)
                .ToListAsync(ct);

            foreach (var content in contents)
                content.SetDeepLinkTemplate(deepLink);
        }
        await db.SaveChangesAsync(ct);
    }
}
