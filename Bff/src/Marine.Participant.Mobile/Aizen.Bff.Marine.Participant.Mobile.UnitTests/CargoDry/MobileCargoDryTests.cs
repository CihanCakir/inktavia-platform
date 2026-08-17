using System.Net;
using Aizen.Bff.Marine.Participant.Mobile.Application.CargoDry;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Refit;

namespace Aizen.Bff.Marine.Participant.Mobile.UnitTests.CargoDry;

/// <summary>
/// BE_MO11a — mobile CargoDry BFF surface. Covers the mapper (validate + counts), the vessel-ownership guard on
/// activate (owned forwards identity with NO user id in the body; non-owned never reaches the module), and clean
/// error surfacing when the downstream throws (e.g. a 401 from a missing audience mapper).
/// </summary>
public sealed class MobileCargoDryTests
{
    // (1) validate maps IsValid + token through.
    [Fact]
    public void Validate_maps_isvalid_and_token()
    {
        var module = new CargoDryKitValidationDto
        {
            IsValid = true, ProductName = "CargoDry Pro", ProductCode = "CD-PRO",
            ValidityDays = 365, HasSmartDevice = true,
            ActivationToken = "tok-123", TokenExpiresAt = DateTimeOffset.UnixEpoch.AddMinutes(5),
        };

        var dto = MobileCargoDryMapper.MapValidation(module);

        dto.IsValid.Should().BeTrue();
        dto.ActivationToken.Should().Be("tok-123");
        dto.TokenExpiresAt.Should().Be(module.TokenExpiresAt);
        dto.ProductName.Should().Be("CargoDry Pro");
        dto.HasSmartDevice.Should().BeTrue();
    }

    // (4) GetMyKits maps counts + items.
    [Fact]
    public void MyKits_maps_counts_and_items()
    {
        var remote = new CargoDryMyKitsRemoteResponse
        {
            Total = 3, ActiveCount = 2, ExpiringCount = 1,
            Items = new List<CargoDryKitDto>
            {
                new() { Id = 1, SerialNumber = "SN1", KitCode = "K1", ProductName = "P", Status = CargoDryKitStatus.Activated },
                new() { Id = 2, SerialNumber = "SN2", KitCode = "K2", ProductName = "P", Status = CargoDryKitStatus.Expired },
            },
        };

        var dto = MobileCargoDryMapper.MapMyKits(remote);

        dto.Total.Should().Be(3);
        dto.ActiveCount.Should().Be(2);
        dto.ExpiringCount.Should().Be(1);
        dto.Items.Should().HaveCount(2);
        dto.Items[0].Status.Should().Be("Activated", "enum status is stringified for the app");
    }

    // (2) activate onto an OWNED vessel succeeds and forwards identity — no explicit user id in the body.
    [Fact]
    public async Task Activate_owned_vessel_succeeds_and_sends_no_user_id()
    {
        const long ownedVesselId = 42;
        var cargoDry = new FakeCargoDryRemoteCall
        {
            ActivateResponse = new CargoDryKitDto
            {
                Id = 9, SerialNumber = "SN9", KitCode = "K9", ProductName = "P",
                Status = CargoDryKitStatus.Activated, VesselId = ownedVesselId,
            },
        };
        var handler = new ActivateMobileKitCommandHandler(
            new FakeParticipantProfileResolver(profileId: 100),
            new FakeVesselRemoteCall(ownedVesselId),
            cargoDry,
            NullLogger<ActivateMobileKitCommandHandler>.Instance);

        var result = await handler.Handle(
            new ActivateMobileKitCommand("tok-abc", ownedVesselId, ActivationMethod.QrScan), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(9);
        cargoDry.ActivateCallCount.Should().Be(1);
        cargoDry.LastActivateBody!.VesselId.Should().Be(ownedVesselId);
        cargoDry.LastActivateBody.ActivationToken.Should().Be("tok-abc");

        // Identity-forwarding proof: the wire body exposes ONLY token/vessel/method — no user/owner id property.
        var bodyProps = cargoDry.LastActivateBody.GetType().GetProperties().Select(p => p.Name);
        bodyProps.Should().NotContain(n => n.Contains("User", StringComparison.OrdinalIgnoreCase)
                                        || n.Contains("Owner", StringComparison.OrdinalIgnoreCase));
    }

    // (3) activate onto a NON-owned vessel → clean business error, module NEVER hit.
    [Fact]
    public async Task Activate_non_owned_vessel_is_rejected_and_module_not_called()
    {
        var cargoDry = new FakeCargoDryRemoteCall();
        var handler = new ActivateMobileKitCommandHandler(
            new FakeParticipantProfileResolver(profileId: 100),
            new FakeVesselRemoteCall(ownedVesselIds: new long[] { 1, 2, 3 }), // caller does NOT own 999
            cargoDry,
            NullLogger<ActivateMobileKitCommandHandler>.Instance);

        var act = () => handler.Handle(
            new ActivateMobileKitCommand("tok-abc", vesselId: 999, ActivationMethod.QrScan), CancellationToken.None);

        await act.Should().ThrowAsync<AizenBusinessException>();
        cargoDry.ActivateCallCount.Should().Be(0, "the module must never be asked to activate onto a non-owned vessel");
    }

    // (5) a downstream 401 (missing audience mapper) surfaces as a clean business error, not an unhandled 500.
    [Fact]
    public async Task Activate_downstream_401_surfaces_clean_business_error()
    {
        const long ownedVesselId = 42;
        var apiException = await ApiException.Create(
            new HttpRequestMessage(HttpMethod.Post, "http://cargodry-api/api/v1/cargodry/kits/activate"),
            HttpMethod.Post,
            new HttpResponseMessage(HttpStatusCode.Unauthorized),
            new RefitSettings());

        var cargoDry = new FakeCargoDryRemoteCall { ThrowOnActivate = apiException };
        var handler = new ActivateMobileKitCommandHandler(
            new FakeParticipantProfileResolver(profileId: 100),
            new FakeVesselRemoteCall(ownedVesselId),
            cargoDry,
            NullLogger<ActivateMobileKitCommandHandler>.Instance);

        var act = () => handler.Handle(
            new ActivateMobileKitCommand("tok-abc", ownedVesselId, ActivationMethod.QrScan), CancellationToken.None);

        (await act.Should().ThrowAsync<AizenBusinessException>()).Which.Should().NotBeNull();
    }

    // Guard: no participant profile ⇒ activation refused before any vessel/module call.
    [Fact]
    public async Task Activate_without_profile_is_rejected()
    {
        var cargoDry = new FakeCargoDryRemoteCall();
        var handler = new ActivateMobileKitCommandHandler(
            new FakeParticipantProfileResolver(profileId: null),
            new FakeVesselRemoteCall(42),
            cargoDry,
            NullLogger<ActivateMobileKitCommandHandler>.Instance);

        var act = () => handler.Handle(
            new ActivateMobileKitCommand("tok-abc", 42, ActivationMethod.QrScan), CancellationToken.None);

        await act.Should().ThrowAsync<AizenBusinessException>();
        cargoDry.ActivateCallCount.Should().Be(0);
    }

    // GetMyKits with no profile ⇒ empty list, never a 500.
    [Fact]
    public async Task MyKits_without_profile_returns_empty()
    {
        var handler = new GetMobileMyKitsQueryHandler(
            new FakeParticipantProfileResolver(profileId: null),
            new FakeCargoDryRemoteCall(),
            NullLogger<GetMobileMyKitsQueryHandler>.Instance);

        var result = await handler.Handle(new GetMobileMyKitsQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().BeEmpty();
        result.Total.Should().Be(0);
    }
}
