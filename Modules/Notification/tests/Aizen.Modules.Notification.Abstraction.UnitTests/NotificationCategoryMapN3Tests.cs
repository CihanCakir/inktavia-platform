using Aizen.Modules.Notification.Abstraction;
using Aizen.Modules.Notification.Abstraction.Enum;
using FluentAssertions;

namespace Aizen.Modules.Notification.Abstraction.UnitTests;

/// <summary>
/// N3 — the new/affected notification types must map to a <b>toggleable</b> category so the N-B per-user
/// preference gate actually applies to them (a type falling into the Account fallback would be always-deliver /
/// non-mutable — the exact trap for a new 133 that lands outside the ServiceRequests range).
/// </summary>
public sealed class NotificationCategoryMapN3Tests
{
    [Theory]
    [InlineData(NotificationType.CompletionApproved,            NotificationCategory.ServiceRequests)] // 131
    [InlineData(NotificationType.MaintenanceReminderDue,        NotificationCategory.ServiceRequests)] // 103 (N2)
    [InlineData(NotificationType.CompletionAutoApproveApproaching, NotificationCategory.ServiceRequests)] // 133 (N3-C)
    [InlineData(NotificationType.DisputeResolved,              NotificationCategory.Disputes)]         // 141 (N3-A)
    [InlineData(NotificationType.ChargebackRecorded,           NotificationCategory.Payments)]         // 159 (N3-B)
    [InlineData(NotificationType.SubscriptionPriceChangeUpcoming, NotificationCategory.Payments)]      // 160 (N1)
    [InlineData(NotificationType.PremiumBoostActivated,        NotificationCategory.Payments)]         // 161 (N4)
    [InlineData(NotificationType.PremiumBoostRevoked,          NotificationCategory.Payments)]         // 162 (N4)
    [InlineData(NotificationType.BenefitBudgetLow,             NotificationCategory.Payments)]         // 163 (N4)
    public void New_types_map_to_the_expected_toggleable_category(NotificationType type, NotificationCategory expected)
    {
        var category = NotificationCategoryMap.Resolve(type);
        category.Should().Be(expected);
        NotificationCategoryMap.ToggleableCategories.Should().Contain(category,
            "the type must be user-mutable via N-B preferences, not silently always-deliver");
        NotificationCategoryMap.IsAlwaysDeliver(category).Should().BeFalse();
    }

    [Fact]
    public void AutoApproveApproaching_is_not_swallowed_by_the_account_fallback()
        => NotificationCategoryMap.Resolve(NotificationType.CompletionAutoApproveApproaching)
            .Should().NotBe(NotificationCategory.Account);
}
