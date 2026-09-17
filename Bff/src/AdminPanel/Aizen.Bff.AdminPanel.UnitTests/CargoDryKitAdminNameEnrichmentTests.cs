using Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryKitDetailBff;
using Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryKitList;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients.CargoDry;
using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Aizen.Bff.AdminPanel.UnitTests;

/// <summary>
/// Admin GEMİ/SAHİP columns showed "—" because the CargoDry module returns VesselId/OwnerUserId but leaves the names
/// null. The kit detail + list handlers now resolve VesselId → vessel name and OwnerUserId → owner display name from
/// the Vessel/Identity modules. When the FKs aren't set, nothing is resolved.
/// </summary>
public sealed class CargoDryKitAdminNameEnrichmentTests
{
    private static AizenApiResponse<List<VesselNameDto>> VesselNames(params (long id, string name)[] rows)
        => new(AizenResponseHeader.Success(), rows.Select(r => new VesselNameDto { VesselId = r.id, Name = r.name }).ToList());

    private static AizenApiResponse<List<UserProfileListItemDto>> Owners(params (long userId, string first, string last)[] rows)
        => new(AizenResponseHeader.Success(),
            rows.Select(r => new UserProfileListItemDto { UserId = r.userId, FirstName = r.first, LastName = r.last }).ToList());

    // ── Detail ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Detail_populates_vessel_and_owner_names_when_fks_are_set()
    {
        var cargoDry = Substitute.For<ICargoDryRemoteCall>();
        cargoDry.GetKitDetailAsync(5, Arg.Any<CancellationToken>()).Returns(new GetCargoDryKitDetailBffResult
        {
            Kit = new CargoDryKitDetailBffDto { Id = 5, VesselId = 2, OwnerUserId = 5 }, // names null from the module
        });

        var vessel = Substitute.For<IVesselRemoteCall>();
        vessel.GetVesselNamesByIds(Arg.Any<long[]>()).Returns(VesselNames((2, "Mavi Selo")));

        var identity = Substitute.For<IIdentityRemoteCall>();
        identity.GetUserProfilesByUserIds(Arg.Any<long[]>()).Returns(Owners((5, "Ada", "Denizci")));

        var resp = await new GetCargoDryKitDetailBffQueryHandler(cargoDry, vessel, identity)
            .Handle(new GetCargoDryKitDetailBffQuery { KitId = 5 }, CancellationToken.None);

        resp.Kit!.VesselName.Should().Be("Mavi Selo");
        resp.Kit.OwnerDisplayName.Should().Be("Ada Denizci");
    }

    [Fact]
    public async Task Detail_does_not_resolve_when_fks_are_absent()
    {
        var cargoDry = Substitute.For<ICargoDryRemoteCall>();
        cargoDry.GetKitDetailAsync(7, Arg.Any<CancellationToken>()).Returns(new GetCargoDryKitDetailBffResult
        {
            Kit = new CargoDryKitDetailBffDto { Id = 7, VesselId = null, OwnerUserId = null },
        });

        var vessel = Substitute.For<IVesselRemoteCall>();
        var identity = Substitute.For<IIdentityRemoteCall>();

        var resp = await new GetCargoDryKitDetailBffQueryHandler(cargoDry, vessel, identity)
            .Handle(new GetCargoDryKitDetailBffQuery { KitId = 7 }, CancellationToken.None);

        resp.Kit!.VesselName.Should().BeNull();
        resp.Kit.OwnerDisplayName.Should().BeNull();
        await vessel.DidNotReceive().GetVesselNamesByIds(Arg.Any<long[]>());
        await identity.DidNotReceive().GetUserProfilesByUserIds(Arg.Any<long[]>());
    }

    // ── List ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task List_populates_gemi_sahip_columns_for_rows_with_fks()
    {
        var cargoDry = Substitute.For<ICargoDryRemoteCall>();
        cargoDry.GetKitsAsync(default, default, default, default, default, default, default, default)
            .ReturnsForAnyArgs(new CargoDryKitListBffDto
            {
                Items = new List<CargoDryKitBffDto>
                {
                    new() { Id = 1, VesselId = 2, OwnerUserId = 5 },
                    new() { Id = 2, VesselId = 3, OwnerUserId = 5 }, // same owner, different vessel
                    new() { Id = 3, VesselId = null, OwnerUserId = null }, // unattached — stays "—"
                },
                Total = 3,
            });

        var vessel = Substitute.For<IVesselRemoteCall>();
        vessel.GetVesselNamesByIds(Arg.Any<long[]>()).Returns(VesselNames((2, "Mavi Selo"), (3, "Kara Yıldız")));

        var identity = Substitute.For<IIdentityRemoteCall>();
        identity.GetUserProfilesByUserIds(Arg.Any<long[]>()).Returns(Owners((5, "Ada", "Denizci")));

        var resp = await new GetCargoDryKitListBffQueryHandler(cargoDry, vessel, identity)
            .Handle(new GetCargoDryKitListBffQuery(), CancellationToken.None);

        var items = resp.KitList.Items;
        items[0].VesselName.Should().Be("Mavi Selo");
        items[0].OwnerDisplayName.Should().Be("Ada Denizci");
        items[1].VesselName.Should().Be("Kara Yıldız");
        items[1].OwnerDisplayName.Should().Be("Ada Denizci");
        items[2].VesselName.Should().BeNull();
        items[2].OwnerDisplayName.Should().BeNull();

        // Batched: one Vessel call + one Identity call for the whole page (distinct ids), not per-row.
        await vessel.Received(1).GetVesselNamesByIds(Arg.Any<long[]>());
        await identity.Received(1).GetUserProfilesByUserIds(Arg.Any<long[]>());
    }
}
