using Aizen.Modules.ServiceRequest.Application.Maintenance;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// S12/N2 — the maintenance interval/lead/batch and the auto-create flag are config-driven (deploy-time tunable, no
/// hardcoded magic constants) with documented defaults; invalid values fall back to the defaults.
/// </summary>
public sealed class MaintenanceScheduleOptionsTests
{
    private static IConfiguration Config(params (string Key, string Value)[] kv)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(kv.Select(p => new KeyValuePair<string, string?>(p.Key, p.Value)))
            .Build();

    [Fact]
    public void Defaults_apply_when_unset()
    {
        var c = Config();
        MaintenanceScheduleOptions.DefaultInterval(c).Should().Be(MaintenanceScheduleOptions.DefaultIntervalMonths);
        MaintenanceScheduleOptions.DefaultLeadDays(c).Should().Be(MaintenanceScheduleOptions.DefaultReminderLeadDays);
        MaintenanceScheduleOptions.ReminderBatchSize(c).Should().Be(MaintenanceScheduleOptions.DefaultBatchSize);
        MaintenanceScheduleOptions.AutoCreateOnCompletion(c).Should().BeFalse("auto-create is off for MVP");
    }

    [Fact]
    public void Config_overrides_the_interval_lead_and_flag()
    {
        var c = Config(
            ("ServiceRequest:MaintenanceDefaultIntervalMonths", "6"),
            ("ServiceRequest:MaintenanceDefaultReminderLeadDays", "30"),
            ("ServiceRequest:MaintenanceReminderBatchSize", "50"),
            ("ServiceRequest:MaintenanceAutoCreateOnCompletion", "true"));

        MaintenanceScheduleOptions.DefaultInterval(c).Should().Be(6);
        MaintenanceScheduleOptions.DefaultLeadDays(c).Should().Be(30);
        MaintenanceScheduleOptions.ReminderBatchSize(c).Should().Be(50);
        MaintenanceScheduleOptions.AutoCreateOnCompletion(c).Should().BeTrue();
    }

    [Fact]
    public void Invalid_values_fall_back_to_defaults()
    {
        // interval must be ≥ 1, lead ≥ 0, batch ≥ 1.
        var c = Config(
            ("ServiceRequest:MaintenanceDefaultIntervalMonths", "0"),
            ("ServiceRequest:MaintenanceDefaultReminderLeadDays", "-5"),
            ("ServiceRequest:MaintenanceReminderBatchSize", "0"));

        MaintenanceScheduleOptions.DefaultInterval(c).Should().Be(MaintenanceScheduleOptions.DefaultIntervalMonths);
        MaintenanceScheduleOptions.DefaultLeadDays(c).Should().Be(MaintenanceScheduleOptions.DefaultReminderLeadDays);
        MaintenanceScheduleOptions.ReminderBatchSize(c).Should().Be(MaintenanceScheduleOptions.DefaultBatchSize);
    }
}
