using Aizen.Bff.Marine.Participant.Mobile.Application.CargoDry;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Bff.Marine.Participant.Mobile.UnitTests.CargoDry;

/// <summary>
/// BE_MO11a read-path fix — GET /vessels/{id}/cargodry. Built from the caller's OWN GetMyKits set filtered to one
/// vessel, so it is owner-scoped by construction and never leaks another owner's kits. ActiveCount &gt; 0 ⇒ the vessel
/// screen shows "protected".
/// </summary>
public sealed class MobileVesselCargoDryTests
{
    private static CargoDryKitDto Kit(long id, long? vesselId, CargoDryKitStatus status, int daysUntilExpiry = 100) => new()
    {
        Id = id, SerialNumber = $"SN{id}", KitCode = $"K{id}", ProductCode = "STANDARD-90",
        ProductName = "CargoDry Standard", BatchCode = "202506-STAN-DEV1", Status = status,
        VesselId = vesselId, EfficiencyPercent = 90, DaysUntilExpiry = daysUntilExpiry, RenewalCount = 0,
        ActivatedAt = DateTimeOffset.UnixEpoch, ExpiresAt = DateTimeOffset.UnixEpoch.AddDays(daysUntilExpiry),
    };

    private static GetMobileVesselCargoDryQueryHandler Handler(FakeCargoDryRemoteCall cargoDry, long? profileId = 100)
        => new(
            new FakeParticipantProfileResolver(profileId),
            cargoDry,
            NullLogger<GetMobileVesselCargoDryQueryHandler>.Instance);

    private static FakeCargoDryRemoteCall WithKits(params CargoDryKitDto[] kits) => new()
    {
        MyKitsResponse = new CargoDryMyKitsRemoteResponse
        {
            Items = kits.ToList(), Total = kits.Length, ActiveCount = kits.Length,
        },
    };

    [Fact]
    public async Task Vessel_with_an_active_kit_reports_it_protected()
    {
        // Two of the caller's kits: one on vessel 2 (Activated), one on vessel 77 — filtering to vessel 2 must keep only its kit.
        var cargoDry = WithKits(
            Kit(1, vesselId: 2, CargoDryKitStatus.Activated, daysUntilExpiry: 340),
            Kit(2, vesselId: 77, CargoDryKitStatus.Activated));

        var dto = await Handler(cargoDry).Handle(new GetMobileVesselCargoDryQuery(2), CancellationToken.None);

        dto.ActiveCount.Should().Be(1, "the vessel has one actively-protecting kit → protected");
        dto.TotalCount.Should().Be(1);
        dto.Kits.Should().ContainSingle();
        dto.Kits[0].Id.Should().Be(1);
        dto.Kits[0].Status.Should().Be("Activated");
        dto.Kits[0].DaysUntilExpiry.Should().Be(340);
        dto.Kits[0].ProductName.Should().Be("CargoDry Standard");
    }

    [Fact]
    public async Task Vessel_without_a_kit_reports_zero()
    {
        var cargoDry = WithKits(Kit(1, vesselId: 2, CargoDryKitStatus.Activated));

        var dto = await Handler(cargoDry).Handle(new GetMobileVesselCargoDryQuery(999), CancellationToken.None);

        dto.ActiveCount.Should().Be(0);
        dto.TotalCount.Should().Be(0);
        dto.Kits.Should().BeEmpty();
    }

    [Fact]
    public async Task Renewed_counts_as_active_but_expired_does_not()
    {
        var cargoDry = WithKits(
            Kit(1, vesselId: 2, CargoDryKitStatus.Renewed),
            Kit(2, vesselId: 2, CargoDryKitStatus.Expired));

        var dto = await Handler(cargoDry).Handle(new GetMobileVesselCargoDryQuery(2), CancellationToken.None);

        dto.TotalCount.Should().Be(2, "both kits are on the vessel");
        dto.ActiveCount.Should().Be(1, "only the Renewed kit is actively protecting; Expired does not count");
    }

    [Fact]
    public async Task No_participant_profile_yields_empty_summary()
    {
        var cargoDry = WithKits(Kit(1, vesselId: 2, CargoDryKitStatus.Activated));

        var dto = await Handler(cargoDry, profileId: null).Handle(new GetMobileVesselCargoDryQuery(2), CancellationToken.None);

        dto.ActiveCount.Should().Be(0);
        dto.Kits.Should().BeEmpty();
    }
}
