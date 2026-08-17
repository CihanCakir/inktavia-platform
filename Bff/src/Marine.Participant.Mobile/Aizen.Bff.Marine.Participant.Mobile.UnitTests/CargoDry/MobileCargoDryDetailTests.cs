using Aizen.Bff.Marine.Participant.Mobile.Application.CargoDry;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Bff.Marine.Participant.Mobile.UnitTests.CargoDry;

/// <summary>
/// BE_MO11b — owner-gated kit detail. Detail is served from the owner's OWN GetMyKits set (never the admin endpoint):
/// an owned kit returns a curated DTO with no admin fields + BFF-enriched vessel name + IsExpiringSoon; a non-owned id
/// is "Kit not found".
/// </summary>
public sealed class MobileCargoDryDetailTests
{
    private static CargoDryKitDto Kit(long id, long? vesselId, CargoDryKitStatus status, int daysUntilExpiry) => new()
    {
        Id = id, SerialNumber = $"SN{id}", KitCode = $"K{id}", ProductCode = "STANDARD-90",
        ProductName = "CargoDry Standard", BatchCode = "202506-STAN-DEV1", Status = status,
        VesselId = vesselId, EfficiencyPercent = 87.5, DaysUntilExpiry = daysUntilExpiry, RenewalCount = 1,
        ActivatedAt = DateTimeOffset.UnixEpoch, ExpiresAt = DateTimeOffset.UnixEpoch.AddDays(daysUntilExpiry),
    };

    private static GetMobileMyKitDetailQueryHandler Handler(FakeCargoDryRemoteCall cargoDry, params long[] ownedVessels)
        => new(
            new FakeParticipantProfileResolver(profileId: 100),
            cargoDry,
            new FakeVesselRemoteCall(ownedVessels),
            NullLogger<GetMobileMyKitDetailQueryHandler>.Instance);

    // (1) detail for an OWNED kit → curated dto with correct efficiency/expiry/renewal, no admin fields.
    [Fact]
    public async Task Detail_for_owned_kit_returns_curated_dto()
    {
        var cargoDry = new FakeCargoDryRemoteCall
        {
            MyKitsResponse = new CargoDryMyKitsRemoteResponse
            {
                Total = 1, ActiveCount = 1,
                Items = new List<CargoDryKitDto> { Kit(9, vesselId: 42, CargoDryKitStatus.Activated, daysUntilExpiry: 100) },
            },
        };

        var dto = await Handler(cargoDry, ownedVessels: 42).Handle(new GetMobileMyKitDetailQuery(9), CancellationToken.None);

        dto.Id.Should().Be(9);
        dto.EfficiencyPercent.Should().Be(87.5);
        dto.DaysUntilExpiry.Should().Be(100);
        dto.RenewalCount.Should().Be(1);
        dto.Status.Should().Be("Activated");
        dto.VesselName.Should().Be("Vessel 42", "vessel name is enriched from the owned-vessel set");
        dto.IsExpiringSoon.Should().BeFalse("100 days is outside the 30-day window");

        // No admin-sensitive fields leak onto the owner contract.
        var props = typeof(MobileKitDetailDto).GetProperties().Select(p => p.Name);
        props.Should().NotContain(n =>
            n.Contains("QrPayload", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Consignment", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("Revoke", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("CommercialModel", StringComparison.OrdinalIgnoreCase) ||
            n.Contains("ProviderProfileId", StringComparison.OrdinalIgnoreCase));
    }

    // (2) detail for a NON-owned kit id → "Kit not found" (the id is not in the caller's GetMyKits set).
    [Fact]
    public async Task Detail_for_non_owned_kit_is_not_found()
    {
        var cargoDry = new FakeCargoDryRemoteCall
        {
            MyKitsResponse = new CargoDryMyKitsRemoteResponse
            {
                Total = 1, Items = new List<CargoDryKitDto> { Kit(9, 42, CargoDryKitStatus.Activated, 100) },
            },
        };

        var act = () => Handler(cargoDry, 42).Handle(new GetMobileMyKitDetailQuery(kitId: 999), CancellationToken.None);

        await act.Should().ThrowAsync<AizenBusinessException>().WithMessage("*not found*");
    }

    // (3) IsExpiringSoon — true only when Activated AND within the 30-day window.
    [Theory]
    [InlineData(CargoDryKitStatus.Activated, 30, true)]
    [InlineData(CargoDryKitStatus.Activated, 5, true)]
    [InlineData(CargoDryKitStatus.Activated, 31, false)]
    [InlineData(CargoDryKitStatus.Expired, 5, false)]   // not Activated
    [InlineData(CargoDryKitStatus.Available, 5, false)]
    public void IsExpiringSoon_follows_module_threshold(CargoDryKitStatus status, int days, bool expected)
    {
        var dto = MobileCargoDryMapper.MapDetail(Kit(1, 42, status, days), vesselName: null);
        dto.IsExpiringSoon.Should().Be(expected);
    }

    // (4) vessel-name enrich → null when the kit's vessel is not in the owned set (or the kit has no vessel).
    [Fact]
    public async Task Detail_vessel_name_null_when_vessel_not_owned()
    {
        var cargoDry = new FakeCargoDryRemoteCall
        {
            MyKitsResponse = new CargoDryMyKitsRemoteResponse
            {
                Total = 1, Items = new List<CargoDryKitDto> { Kit(9, vesselId: 77, CargoDryKitStatus.Activated, 100) },
            },
        };

        // Caller owns vessel 42, not 77 (the kit's vessel) — enrich yields null, detail still returns.
        var dto = await Handler(cargoDry, ownedVessels: 42).Handle(new GetMobileMyKitDetailQuery(9), CancellationToken.None);

        dto.Id.Should().Be(9);
        dto.VesselName.Should().BeNull();
    }

    // Guard: no participant profile ⇒ not found (no kit/vessel calls).
    [Fact]
    public async Task Detail_without_profile_is_not_found()
    {
        var cargoDry = new FakeCargoDryRemoteCall();
        var handler = new GetMobileMyKitDetailQueryHandler(
            new FakeParticipantProfileResolver(profileId: null),
            cargoDry,
            new FakeVesselRemoteCall(42),
            NullLogger<GetMobileMyKitDetailQueryHandler>.Instance);

        var act = () => handler.Handle(new GetMobileMyKitDetailQuery(9), CancellationToken.None);

        await act.Should().ThrowAsync<AizenBusinessException>().WithMessage("*not found*");
    }
}
