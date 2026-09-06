using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Application.Services.Trip;
using Aizen.Modules.ServiceRequest.Domain.Entities.Trip;
using FluentAssertions;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

public sealed class TripTrackingTests
{
    // ── State machine guards ─────────────────────────────────────────────────

    [Fact]
    public void Start_Opens_As_EnRoute()
    {
        var trip = ServiceRequestTripEntity.Start(serviceRequestId: 5, providerProfileId: 9);
        trip.Status.Should().Be(TripStatus.EnRoute);
        trip.IsEnRoute.Should().BeTrue();
        trip.ServiceRequestId.Should().Be(5);
        trip.ProviderProfileId.Should().Be(9);
        trip.ArrivedAt.Should().BeNull();
    }

    [Fact]
    public void Ping_Updates_LastKnown_While_EnRoute()
    {
        var trip = ServiceRequestTripEntity.Start(1, 2);
        var at = new DateTime(2026, 9, 6, 10, 0, 0, DateTimeKind.Utc);
        trip.Ping(40.9739m, 29.0355m, 120m, at);
        trip.LastLatitude.Should().Be(40.9739m);
        trip.LastLongitude.Should().Be(29.0355m);
        trip.LastHeading.Should().Be(120m);
        trip.LastPingAt.Should().Be(at);
    }

    [Fact]
    public void Ping_After_Arrive_Throws_TripNotEnRoute()
    {
        var trip = ServiceRequestTripEntity.Start(1, 2);
        trip.Arrive();
        var act = () => trip.Ping(1m, 1m, null, DateTime.UtcNow);
        act.Should().Throw<AizenBusinessException>().WithMessage("TRIP_NOT_ENROUTE");
    }

    [Fact]
    public void Arrive_From_EnRoute_Is_Terminal()
    {
        var trip = ServiceRequestTripEntity.Start(1, 2);
        trip.Arrive();
        trip.Status.Should().Be(TripStatus.Arrived);
        trip.ArrivedAt.Should().NotBeNull();
    }

    [Fact]
    public void Arrive_When_Already_Terminal_Throws()
    {
        var trip = ServiceRequestTripEntity.Start(1, 2);
        trip.Cancel();
        var act = () => trip.Arrive();
        act.Should().Throw<AizenBusinessException>().WithMessage("TRIP_NOT_ENROUTE");
    }

    [Fact]
    public void Cancel_From_EnRoute_Is_Terminal()
    {
        var trip = ServiceRequestTripEntity.Start(1, 2);
        trip.Cancel();
        trip.Status.Should().Be(TripStatus.Cancelled);
        trip.CancelledAt.Should().NotBeNull();
    }

    [Fact]
    public void RestartEnRoute_On_Active_Trip_Throws()
    {
        var trip = ServiceRequestTripEntity.Start(1, 2);
        var act = () => trip.RestartEnRoute(2);
        act.Should().Throw<AizenBusinessException>().WithMessage("TRIP_ALREADY_ACTIVE");
    }

    [Fact]
    public void RestartEnRoute_On_Terminal_Trip_Rearms_And_Clears()
    {
        var trip = ServiceRequestTripEntity.Start(1, 2);
        trip.Ping(10m, 10m, 90m, DateTime.UtcNow);
        trip.Arrive();
        trip.SetSummary(3.2m, 600);

        trip.RestartEnRoute(7);

        trip.Status.Should().Be(TripStatus.EnRoute);
        trip.ProviderProfileId.Should().Be(7);
        trip.ArrivedAt.Should().BeNull();
        trip.LastLatitude.Should().BeNull();
        trip.TotalDistanceKm.Should().BeNull();
        trip.DurationSeconds.Should().BeNull();
    }

    // ── Throttle ─────────────────────────────────────────────────────────────

    [Fact]
    public void Throttle_Ignores_Pings_Under_3s()
    {
        var now = new DateTime(2026, 9, 6, 10, 0, 3, DateTimeKind.Utc);
        TripMath.ShouldThrottle(now.AddSeconds(-1), now).Should().BeTrue();   // 1s apart → drop
        TripMath.ShouldThrottle(now.AddSeconds(-2.9), now).Should().BeTrue(); // <3s → drop
    }

    [Fact]
    public void Throttle_Allows_Pings_At_Or_Beyond_3s_And_First_Ping()
    {
        var now = new DateTime(2026, 9, 6, 10, 0, 3, DateTimeKind.Utc);
        TripMath.ShouldThrottle(now.AddSeconds(-3), now).Should().BeFalse();  // exactly 3s → keep
        TripMath.ShouldThrottle(now.AddSeconds(-10), now).Should().BeFalse();
        TripMath.ShouldThrottle(null, now).Should().BeFalse();                // first ping never throttled
    }

    // ── Summary + ETA math (null-safe, coarse) ───────────────────────────────

    [Fact]
    public void ComputeSummary_Sums_Distance_And_Duration()
    {
        var t0 = new DateTime(2026, 9, 6, 10, 0, 0, DateTimeKind.Utc);
        var positions = new List<ServiceRequestTripPositionEntity>
        {
            ServiceRequestTripPositionEntity.Create(1, 40.9700m, 29.0300m, null, t0),
            ServiceRequestTripPositionEntity.Create(1, 40.9800m, 29.0400m, null, t0.AddMinutes(2)),
        };
        var (km, seconds) = TripMath.ComputeSummary(positions);
        km.Should().BeGreaterThan(0m);
        seconds.Should().Be(120);
    }

    [Fact]
    public void ComputeEta_Is_Null_Without_Enough_History_Or_Location()
    {
        // No positions → no speed → null.
        TripMath.ComputeEtaMinutes(40m, 29m, 41m, 29m, new List<ServiceRequestTripPositionEntity>())
            .Should().BeNull();
        // Missing job location → null.
        var t0 = new DateTime(2026, 9, 6, 10, 0, 0, DateTimeKind.Utc);
        var recent = new List<ServiceRequestTripPositionEntity>
        {
            ServiceRequestTripPositionEntity.Create(1, 40.99m, 29.05m, null, t0.AddMinutes(1)),
            ServiceRequestTripPositionEntity.Create(1, 40.97m, 29.03m, null, t0),
        };
        TripMath.ComputeEtaMinutes(40.99m, 29.05m, null, null, recent).Should().BeNull();
    }

    [Fact]
    public void ComputeEta_Returns_Positive_When_Moving_Toward_Target()
    {
        var t0 = new DateTime(2026, 9, 6, 10, 0, 0, DateTimeKind.Utc);
        // newest→oldest, ~moving north over 1 minute.
        var recent = new List<ServiceRequestTripPositionEntity>
        {
            ServiceRequestTripPositionEntity.Create(1, 41.00m, 29.00m, null, t0.AddMinutes(1)),
            ServiceRequestTripPositionEntity.Create(1, 40.90m, 29.00m, null, t0),
        };
        var eta = TripMath.ComputeEtaMinutes(41.00m, 29.00m, 41.50m, 29.00m, recent);
        eta.Should().NotBeNull();
        eta!.Value.Should().BeGreaterThan(0);
    }
}
