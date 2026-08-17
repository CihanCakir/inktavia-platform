using System.Reflection;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Repository.Seed;
using FluentAssertions;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>
/// BE-MO9c — the owner event-set template gap-fill. A NotificationType with no template is a SILENT no-op, so every
/// owner-relevant type must have a template for the owner to actually receive it. These verify the four previously
/// missing types now have an InApp template (OfferRejected / CompletionRejected / PaymentCaptured / PaymentRefunded),
/// that the whole owner set is covered, and that the seed is duplicate-safe by TemplateCode.
/// </summary>
public sealed class OwnerEventSetSeedMo9cTests
{
    // BuildTemplates() is private static — reflect it (it returns List<NotificationTemplateEntity>).
    private static IReadOnlyList<NotificationTemplateEntity> Templates()
    {
        var m = typeof(NotificationTemplateSeed).GetMethod("BuildTemplates", BindingFlags.NonPublic | BindingFlags.Static)!;
        return ((IEnumerable<NotificationTemplateEntity>)m.Invoke(null, null)!).ToList();
    }

    // The four types that were MISSING a template before MO9c → now present as InApp.
    [Theory]
    [InlineData(NotificationType.OfferRejected)]
    [InlineData(NotificationType.CompletionRejected)]
    [InlineData(NotificationType.PaymentCaptured)]
    [InlineData(NotificationType.PaymentRefunded)]
    public void Gap_filled_owner_types_now_have_an_inapp_template(NotificationType type)
    {
        Templates().Should().Contain(t => t.Type == type && t.Channel == NotificationChannel.InApp,
            $"{type} must have an InApp template or it is a silent no-op");
    }

    // The whole owner event set has at least one template (no silent no-ops for the owner bell/inbox).
    [Fact]
    public void Whole_owner_event_set_is_covered()
    {
        var ownerSet = new[]
        {
            NotificationType.OfferCreated, NotificationType.OfferAccepted, NotificationType.OfferRejected,
            NotificationType.CompletionSubmitted, NotificationType.CompletionApproved, NotificationType.CompletionRejected,
            NotificationType.CompletionAutoApproveApproaching,
            NotificationType.DisputeOpened, NotificationType.DisputeResolved,
            NotificationType.PaymentCaptured, NotificationType.PaymentRefunded, NotificationType.PaymentReleased,
            NotificationType.PaymentAuthorized,
            NotificationType.MaintenanceReminderDue, NotificationType.SubscriptionPriceChangeUpcoming,
            NotificationType.NewMessageReceived,
        };

        var covered = Templates().Select(t => t.Type).ToHashSet();
        ownerSet.Should().OnlyContain(t => covered.Contains(t), "every owner-relevant type needs a template");
    }

    // Idempotent seed: TemplateCodes are unique, so a re-run inserts nothing (the seed guards by AnyAsync(TemplateCode)).
    [Fact]
    public void Template_codes_are_unique_so_reseeding_is_a_noop()
    {
        var codes = Templates().Select(t => t.TemplateCode).ToList();
        codes.Should().OnlyHaveUniqueItems("the seed dedupes by TemplateCode — duplicates would double-insert");
    }
}
