using Aizen.Modules.ServiceRequest.Application.Completion;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// N3-C — the auto-approval knobs are config-driven (deploy-time tunable, no hardcoded magic constants) with documented
/// defaults, and the reminder lead is always clamped strictly inside the window.
/// </summary>
public sealed class CompletionAutoApprovalOptionsTests
{
    private static IConfiguration Config(params (string Key, string Value)[] kv)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(kv.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value)))
            .Build();

    [Fact]
    public void Defaults_apply_when_unset()
    {
        var c = Config();
        CompletionAutoApprovalOptions.WindowDays(c).Should().Be(CompletionAutoApprovalOptions.DefaultWindowDays);
        CompletionAutoApprovalOptions.ReminderLeadDays(c).Should().Be(CompletionAutoApprovalOptions.DefaultReminderLeadDays);
        CompletionAutoApprovalOptions.BatchSize(c).Should().Be(CompletionAutoApprovalOptions.DefaultBatchSize);
        CompletionAutoApprovalOptions.SystemActorUserId(c).Should().Be(0);
    }

    [Fact]
    public void Config_overrides_the_window_and_lead()
    {
        var c = Config(
            ("ServiceRequest:AutoApproveWindowDays", "10"),
            ("ServiceRequest:AutoApproveReminderLeadDays", "3"),
            ("ServiceRequest:AutoApproveSystemActorUserId", "42"));

        CompletionAutoApprovalOptions.WindowDays(c).Should().Be(10);
        CompletionAutoApprovalOptions.ReminderLeadDays(c).Should().Be(3);
        CompletionAutoApprovalOptions.SystemActorUserId(c).Should().Be(42);
    }

    [Fact]
    public void Reminder_lead_is_clamped_below_the_window()
    {
        // A lead ≥ window would fire the reminder immediately at submission — clamp to window-1.
        var c = Config(("ServiceRequest:AutoApproveWindowDays", "3"), ("ServiceRequest:AutoApproveReminderLeadDays", "9"));
        CompletionAutoApprovalOptions.ReminderLeadDays(c).Should().Be(2); // window(3) - 1
    }

    [Fact]
    public void Invalid_window_falls_back_to_default()
    {
        var c = Config(("ServiceRequest:AutoApproveWindowDays", "0"));
        CompletionAutoApprovalOptions.WindowDays(c).Should().Be(CompletionAutoApprovalOptions.DefaultWindowDays);
    }

    [Fact]
    public void ComputeAutoApproveAt_is_submitted_plus_window()
    {
        var submitted = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        CompletionAutoApprovalOptions.ComputeAutoApproveAt(submitted, 7)
            .Should().Be(new DateTime(2026, 1, 8, 12, 0, 0, DateTimeKind.Utc));
    }
}
