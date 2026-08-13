using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.ServiceRequest.Abstraction.Dto;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Aizen.Bff.AdminPanel.UnitTests;

/// <summary>
/// BE-QA3 — the SR admin list + detail id→name resolution happens at the BFF (the modules stay boundary-pure).
/// These pin the composition: the list handler maps VesselName/ProviderName from the Vessel + Identity batches,
/// an unassigned row gets a null provider (FE shows "Unassigned"), a failed resolver degrades to null names + a
/// warning (never an exception), and the detail handler resolves the owner name inside the SAME identity batch.
/// </summary>
public sealed class ServiceRequestNameResolutionTests
{
    private static AizenApiResponse<T> Ok<T>(T body) where T : class => new(AizenResponseHeader.Success(), body);

    private static ServiceRequestSummaryDto Summary(long id, long vesselId, long ownerUserId, long? providerUserId) => new()
    {
        Id = id,
        RequestCode = $"SR{id}",
        Title = $"Request {id}",
        ServiceCategoryCode = "ELECTRICAL",
        VesselId = vesselId,
        OwnerUserId = ownerUserId,
        ProviderProfileId = providerUserId is null ? null : 100011,
        ProviderUserId = providerUserId,
        HasActiveAssignment = providerUserId is not null,
    };

    private static UserProfileListItemDto Profile(long userId, string first, string last) =>
        new() { UserId = userId, FirstName = first, LastName = last };

    private static (GetServiceRequestListBffQueryHandler handler, IVesselRemoteCall vessel, IIdentityRemoteCall identity)
        BuildListHandler(IServiceRequestRemoteCall serviceRequest)
    {
        var vessel = Substitute.For<IVesselRemoteCall>();
        var identity = Substitute.For<IIdentityRemoteCall>();
        return (new GetServiceRequestListBffQueryHandler(serviceRequest, vessel, identity), vessel, identity);
    }

    // ── LIST: maps vessel + provider names; unassigned row → null provider ───────────────────────
    [Fact]
    public async Task List_maps_vessel_and_provider_names_and_leaves_unassigned_provider_null()
    {
        var sr = Substitute.For<IServiceRequestRemoteCall>();
        sr.GetAdminServiceRequestList(Arg.Any<string?>(), Arg.Any<long?>(), Arg.Any<long?>(), Arg.Any<int>(), Arg.Any<int>())
          .Returns(Ok(new GetAdminServiceRequestListResponse(
              new List<ServiceRequestSummaryDto>
              {
                  Summary(id: 1, vesselId: 100014, ownerUserId: 100029, providerUserId: 100011), // assigned
                  Summary(id: 2, vesselId: 100014, ownerUserId: 100029, providerUserId: null),   // unassigned
              },
              totalCount: 2)));

        var (handler, vessel, identity) = BuildListHandler(sr);
        vessel.GetVesselNamesByIds(Arg.Any<long[]>())
              .Returns(Ok(new List<VesselNameDto> { new() { VesselId = 100014, Name = "Aegean Wind" } }));
        identity.GetUserProfilesByUserIds(Arg.Any<long[]>())
                .Returns(Ok(new List<UserProfileListItemDto> { Profile(100011, "Cihan", "Çakır") }));

        var result = await handler.Handle(new GetServiceRequestListBffQuery(null, null, null, 0, 20), CancellationToken.None);

        var items = result!.ServiceRequests!.Items;
        items.Should().HaveCount(2);
        items[0].VesselName.Should().Be("Aegean Wind");
        items[1].VesselName.Should().Be("Aegean Wind"); // shared vessel resolved from the deduped batch
        items[0].ProviderName.Should().Be("Cihan Çakır");
        items[1].ProviderName.Should().BeNull();        // no assignment → FE shows "Unassigned"
        result.Warnings.Should().BeEmpty();

        // No N+1: the deduped vessel set is a single id → one batch call with exactly [100014].
        await vessel.Received(1).GetVesselNamesByIds(Arg.Is<long[]>(ids => ids.Length == 1 && ids[0] == 100014));
    }

    // ── LIST: a failed vessel resolver degrades to null names + a warning (never throws) ─────────
    [Fact]
    public async Task List_failed_vessel_resolver_yields_null_vessel_name_and_warning()
    {
        var sr = Substitute.For<IServiceRequestRemoteCall>();
        sr.GetAdminServiceRequestList(Arg.Any<string?>(), Arg.Any<long?>(), Arg.Any<long?>(), Arg.Any<int>(), Arg.Any<int>())
          .Returns(Ok(new GetAdminServiceRequestListResponse(
              new List<ServiceRequestSummaryDto> { Summary(1, 100014, 100029, 100011) }, totalCount: 1)));

        var (handler, vessel, identity) = BuildListHandler(sr);
        vessel.GetVesselNamesByIds(Arg.Any<long[]>()).Throws(new Exception("Vessel down"));
        identity.GetUserProfilesByUserIds(Arg.Any<long[]>())
                .Returns(Ok(new List<UserProfileListItemDto> { Profile(100011, "Cihan", "Çakır") }));

        var result = await handler.Handle(new GetServiceRequestListBffQuery(null, null, null, 0, 20), CancellationToken.None);

        var row = result!.ServiceRequests!.Items.Single();
        row.VesselName.Should().BeNull();               // failed resolve → null, FE falls back to the id
        row.ProviderName.Should().Be("Cihan Çakır");    // identity still succeeded
        result.Warnings.Should().Contain(w => w.Module == "Vessel"); // degraded gracefully, no exception
    }

    // ── DETAIL: the owner name is resolved inside the SAME identity batch as the providers ───────
    [Fact]
    public async Task Detail_resolves_owner_name_in_the_single_identity_batch()
    {
        const long ownerUserId = 100029;
        const long providerUserId = 100011;

        var sr = Substitute.For<IServiceRequestRemoteCall>();
        var detail = new ServiceRequestDetailDto
        {
            Request = new ServiceRequestDto { Id = 9011, OwnerUserId = ownerUserId, VesselId = 100014 },
            Offers = new List<ServiceRequestOfferDto> { new() { ProviderUserId = providerUserId } },
        };
        sr.GetAdminServiceRequestDetail(9011).Returns(Ok(new GetServiceRequestDetailResponse(detail)));

        var vessel = Substitute.For<IVesselRemoteCall>();
        var identity = Substitute.For<IIdentityRemoteCall>();
        long[]? batchedIds = null;
        identity.GetUserProfilesByUserIds(Arg.Do<long[]>(ids => batchedIds = ids))
                .Returns(Ok(new List<UserProfileListItemDto>
                {
                    Profile(ownerUserId, "Ada", "Yılmaz"),
                    Profile(providerUserId, "Cihan", "Çakır"),
                }));

        var handler = new GetServiceRequestOperationDetailBffQueryHandler(sr, vessel, identity);
        var result = await handler.Handle(new GetServiceRequestOperationDetailBffQuery(9011), CancellationToken.None);

        result!.OwnerName.Should().Be("Ada Yılmaz");
        result.ProviderNames.Should().ContainKey(providerUserId);
        // ONE batch, and it carried BOTH the owner and the provider id (no extra round-trip).
        await identity.Received(1).GetUserProfilesByUserIds(Arg.Any<long[]>());
        batchedIds.Should().Contain(new[] { ownerUserId, providerUserId });
    }
}
