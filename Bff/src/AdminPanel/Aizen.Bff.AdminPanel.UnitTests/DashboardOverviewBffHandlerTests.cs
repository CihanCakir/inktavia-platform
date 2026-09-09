using System.Reflection;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Dashboard.Query;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.Cache.Abstraction.Common;
using Aizen.Core.Infrastructure.Api;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Admin;
using FluentAssertions;
using MiniUow.Paging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace Aizen.Bff.AdminPanel.UnitTests;

/// <summary>
/// #107 — the admin dashboard-overview endpoint fans out to 5 downstream modules and degraded
/// disproportionately under load. These pin the perf fix WITHOUT changing the response contract:
///   • it is a short-TTL distributed-cached query (collapses concurrent fan-outs into one), and
///   • it fetches only the smallest page (pageSize:1) from each list — the total counts, not the rows.
/// The mapping/graceful-degradation behaviour (best-effort, warning per down module) is preserved.
/// </summary>
public sealed class DashboardOverviewBffHandlerTests
{
    private static AizenApiResponse<T> Ok<T>(T body) where T : class => new(AizenResponseHeader.Success(), body);

    // MiniUow's Paginate<T> exposes only an internal parameterless ctor; Count is the TOTAL record count
    // (independent of page size), which is exactly what the handler reads for TotalVessels.
    private static Paginate<T> PaginateWithCount<T>(int count)
    {
        var ctor = typeof(Paginate<T>).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, Type.EmptyTypes, null)!;
        var paginate = (Paginate<T>)ctor.Invoke(null);
        typeof(Paginate<T>).GetProperty("Count")!.SetValue(paginate, count);
        return paginate;
    }

    private static (GetDashboardOverviewBffQueryHandler handler,
        IIdentityRemoteCall identity, IVesselRemoteCall vessel, IServiceRequestRemoteCall sr) Build()
    {
        var identity = Substitute.For<IIdentityRemoteCall>();
        var vessel = Substitute.For<IVesselRemoteCall>();
        var sr = Substitute.For<IServiceRequestRemoteCall>();
        return (new GetDashboardOverviewBffQueryHandler(identity, vessel, sr), identity, vessel, sr);
    }

    private static void WireHappyPath(
        IIdentityRemoteCall identity, IVesselRemoteCall vessel, IServiceRequestRemoteCall sr,
        int vessels, int activeSr, int disputes, int organizers, int venues)
    {
        vessel.GetAdminVesselList().ReturnsForAnyArgs(
            Ok(new GetAllVesselsAdminResponse(PaginateWithCount<VesselListItemDto>(vessels))));
        sr.GetAdminServiceRequestList().ReturnsForAnyArgs(
            Ok(new GetAdminServiceRequestListResponse(new(), activeSr)));
        sr.GetAdminDisputeList().ReturnsForAnyArgs(
            Ok(new GetAdminDisputeListResponse(new(), disputes)));
        identity.SearchOrganizerProfiles().ReturnsForAnyArgs(
            Ok(new PagedOrganizerProfileResult { TotalCount = organizers }));
        identity.SearchVenueProfiles().ReturnsForAnyArgs(
            Ok(new PagedVenueProfileResult { TotalCount = venues }));
    }

    [Fact]
    public void Overview_is_a_short_ttl_distributed_cached_query()
    {
        var (handler, _, _, _) = Build();

        handler.Should().BeAssignableTo<IAizenQueryHandlerCacheable>();
        var cacheable = (IAizenQueryHandlerCacheable)handler;

        // Distributed so the whole replica set shares one fan-out per window.
        cacheable.CacheType.Should().Be(AizenCacheType.Distributed);

        // Short TTL (15–30s guidance) so N concurrent requests collapse to one fan-out; payload is
        // language-independent numbers, so the language-blind cache key (D-05) is safe.
        var ttl = cacheable.CacheOptions.AbsoluteExpirationRelativeToNow;
        ttl.Should().NotBeNull();
        ttl!.Value.TotalSeconds.Should().BeInRange(15, 30);
    }

    [Fact]
    public async Task Overview_maps_totals_and_requests_only_the_smallest_page()
    {
        var (handler, identity, vessel, sr) = Build();
        WireHappyPath(identity, vessel, sr, vessels: 7, activeSr: 42, disputes: 3, organizers: 5, venues: 2);

        var result = await handler.Handle(new GetDashboardOverviewBffQuery(), CancellationToken.None);

        result!.TotalVessels.Should().Be(7);
        result.TotalActiveServiceRequests.Should().Be(42);
        result.TotalOpenDisputes.Should().Be(3);
        result.PendingOrganizerApprovals.Should().Be(5);
        result.PendingVenueApprovals.Should().Be(2);
        result.Warnings.Should().BeEmpty();

        // Only counts are consumed → each list is fetched with pageSize:1, never the default 20 rows.
        await vessel.Received(1).GetAdminVesselList(
            Arg.Any<int>(), 1, Arg.Any<string?>(), Arg.Any<bool?>(),
            Arg.Any<int[]?>(), Arg.Any<int[]?>(), Arg.Any<int[]?>(), Arg.Any<long?>());
        await identity.Received(1).SearchVenueProfiles(Arg.Any<int>(), 1);
        await sr.Received(1).GetAdminDisputeList(Arg.Any<string?>(), Arg.Any<int>(), 1);
    }

    [Fact]
    public async Task Overview_degrades_to_a_warning_when_a_module_is_down_without_throwing()
    {
        var (handler, identity, vessel, sr) = Build();
        WireHappyPath(identity, vessel, sr, vessels: 7, activeSr: 42, disputes: 3, organizers: 5, venues: 2);
        // Refit surfaces a down module as a FAULTED TASK (not a synchronous throw); the handler awaits all
        // fan-out tasks non-throwing and turns a fault into a warning.
        vessel.GetAdminVesselList().ReturnsForAnyArgs(
            Task.FromException<AizenApiResponse<GetAllVesselsAdminResponse>>(new Exception("Vessel module down")));

        var result = await handler.Handle(new GetDashboardOverviewBffQuery(), CancellationToken.None);

        result!.TotalVessels.Should().Be(0);                                   // degraded
        result.TotalActiveServiceRequests.Should().Be(42);                     // siblings unaffected
        result.Warnings.Should().Contain(w => w.Module == "Vessel");
    }
}
