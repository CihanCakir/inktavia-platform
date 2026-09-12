using Aizen.Modules.Notification.Abstraction;
using Aizen.Modules.Notification.Abstraction.Enum;
using FluentAssertions;

namespace Aizen.Modules.Notification.Abstraction.UnitTests;

/// <summary>
/// N-B preference policy — pins the (category × channel) default matrix, especially the transactional-vs-commercial
/// Email split (owners must get transactional mail without opting in) and the rule that a stored row always wins.
/// </summary>
public sealed class NotificationPreferencePolicyTests
{
    // ── Email default is CATEGORY-AWARE: transactional on, commercial/high-volume opt-in ──────────────────────
    [Theory]
    [InlineData(NotificationCategory.ServiceRequests, true)]  // transactional — the owner-gets-no-email fix
    [InlineData(NotificationCategory.Payments,        true)]  // transactional
    [InlineData(NotificationCategory.Disputes,        true)]  // transactional
    [InlineData(NotificationCategory.Messages,        false)] // opt-in (high-volume chat)
    [InlineData(NotificationCategory.CargoDry,        false)] // opt-in (commercial)
    [InlineData(NotificationCategory.Broadcast,       false)] // opt-in (marketing)
    public void Email_default_follows_the_transactional_vs_commercial_split(NotificationCategory category, bool expected)
        => NotificationPreferencePolicy.DefaultEnabled(category, NotificationChannel.Email).Should().Be(expected);

    [Fact]
    public void Email_default_for_Account_is_always_deliver()
        => NotificationPreferencePolicy.DefaultEnabled(NotificationCategory.Account, NotificationChannel.Email)
            .Should().BeTrue(); // IsAlwaysDeliver short-circuits — security mail is never opt-in

    // ── Push default unchanged: on for every toggleable category ─────────────────────────────────────────────
    [Theory]
    [InlineData(NotificationCategory.ServiceRequests)]
    [InlineData(NotificationCategory.Payments)]
    [InlineData(NotificationCategory.Disputes)]
    [InlineData(NotificationCategory.Messages)]
    [InlineData(NotificationCategory.CargoDry)]
    [InlineData(NotificationCategory.Broadcast)]
    public void Push_default_is_on_for_every_category(NotificationCategory category)
        => NotificationPreferencePolicy.DefaultEnabled(category, NotificationChannel.Push).Should().BeTrue();

    // ── InApp default unchanged: baseline inbox, always on ───────────────────────────────────────────────────
    [Theory]
    [InlineData(NotificationCategory.ServiceRequests)]
    [InlineData(NotificationCategory.Messages)]
    [InlineData(NotificationCategory.Broadcast)]
    public void InApp_default_is_on(NotificationCategory category)
        => NotificationPreferencePolicy.DefaultEnabled(category, NotificationChannel.InApp).Should().BeTrue();

    // ── Resolve prefers the STORED value over the default (both directions) ──────────────────────────────────
    [Fact]
    public void Resolve_uses_the_default_when_no_row_is_stored()
        => NotificationPreferencePolicy.Resolve(NotificationCategory.ServiceRequests, NotificationChannel.Email, stored: null)
            .Should().BeTrue();

    [Fact]
    public void Resolve_stored_disabled_beats_a_transactional_on_by_default_category()
        // A user who explicitly turned ServiceRequests email OFF must stay off — the new default must not override it.
        => NotificationPreferencePolicy.Resolve(NotificationCategory.ServiceRequests, NotificationChannel.Email, stored: false)
            .Should().BeFalse();

    [Fact]
    public void Resolve_stored_enabled_beats_a_commercial_off_by_default_category()
        => NotificationPreferencePolicy.Resolve(NotificationCategory.Broadcast, NotificationChannel.Email, stored: true)
            .Should().BeTrue();

    // ── Locked cells always deliver regardless of a stored "false" ───────────────────────────────────────────
    [Fact]
    public void Resolve_locked_account_email_ignores_a_stored_false()
        => NotificationPreferencePolicy.Resolve(NotificationCategory.Account, NotificationChannel.Email, stored: false)
            .Should().BeTrue();

    [Fact]
    public void Resolve_locked_inapp_ignores_a_stored_false()
        => NotificationPreferencePolicy.Resolve(NotificationCategory.Messages, NotificationChannel.InApp, stored: false)
            .Should().BeTrue();
}
