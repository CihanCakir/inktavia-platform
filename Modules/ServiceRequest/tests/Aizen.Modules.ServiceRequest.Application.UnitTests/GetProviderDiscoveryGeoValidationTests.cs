using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Filter;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;
using Aizen.Modules.ServiceRequest.Application.Query.Provider.GetProviderDiscovery;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// Geo-filter validation on provider discovery.
///
/// Regression for the discovery 500: the app calls discovery with a CENTRE (to compute + sort by distance) but NO
/// radius. The handler used to require centre + radius together, throwing a business 400 that the BFF (Refit,
/// un-caught) surfaced as a 500. RadiusKm is now optional — a centre alone is valid; radius, when present, still
/// requires a centre; centre lat/lng must be paired.
/// </summary>
public sealed class GetProviderDiscoveryGeoValidationTests
{
    private const long ProviderProfileId = 42;

    private static GetProviderDiscoveryQueryHandler BuildHandler(out IServiceRequestRepository repo)
    {
        repo = Substitute.For<IServiceRequestRepository>();
        repo.GetDiscoveryAsync(Arg.Any<long>(), Arg.Any<ProviderServiceRequestDiscoveryFilter>(), Arg.Any<CancellationToken>())
            .Returns(new List<ProviderDiscoveryItemDto>());

        var info = Substitute.For<IAizenInfoAccessor>();
        var kc = Substitute.For<IAizenKeycloakTokenInfoAccessor>();
        kc.KeycloakTokenInfo.Returns(new AizenKeycloakTokenInfo { ProviderProfileId = ProviderProfileId });
        info.KeycloakTokenInfoAccessor.Returns(kc);

        var refData = Substitute.For<IServiceRequestReferenceDataRemoteCall>();

        return new GetProviderDiscoveryQueryHandler(repo, info, refData);
    }

    private static GetProviderDiscoveryQuery Query(ProviderServiceRequestDiscoveryFilter filter) => new() { Filter = filter };

    [Fact]
    public async Task Centre_Without_Radius_Is_Valid()
    {
        var handler = BuildHandler(out var repo);
        var filter = new ProviderServiceRequestDiscoveryFilter
        {
            PageSize = 20,
            SortBy = "PublishedAtDesc",
            CenterLatitude = 40.98796679800172m,
            CenterLongitude = 29.049314494155713m,
            RadiusKm = null, // the exact captured-curl shape
        };

        var result = await handler.Handle(Query(filter), CancellationToken.None);

        result.Should().NotBeNull();
        result!.LocationMode.Should().Be("Geo");
        await repo.Received(1).GetDiscoveryAsync(ProviderProfileId, Arg.Any<ProviderServiceRequestDiscoveryFilter>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Radius_Without_Centre_Is_Rejected()
    {
        var handler = BuildHandler(out _);
        var filter = new ProviderServiceRequestDiscoveryFilter { RadiusKm = 50m };

        var act = () => handler.Handle(Query(filter), CancellationToken.None);

        await act.Should().ThrowAsync<AizenBusinessException>().WithMessage("*RadiusKm requires*");
    }

    [Fact]
    public async Task Centre_Latitude_Without_Longitude_Is_Rejected()
    {
        var handler = BuildHandler(out _);
        var filter = new ProviderServiceRequestDiscoveryFilter { CenterLatitude = 40.9m };

        var act = () => handler.Handle(Query(filter), CancellationToken.None);

        await act.Should().ThrowAsync<AizenBusinessException>().WithMessage("*must be provided together*");
    }

    [Fact]
    public async Task Centre_Plus_Valid_Radius_Still_Works()
    {
        var handler = BuildHandler(out var repo);
        var filter = new ProviderServiceRequestDiscoveryFilter
        {
            CenterLatitude = 40.9m, CenterLongitude = 29.0m, RadiusKm = 25m,
        };

        var result = await handler.Handle(Query(filter), CancellationToken.None);

        result.Should().NotBeNull();
        await repo.Received(1).GetDiscoveryAsync(ProviderProfileId, Arg.Any<ProviderServiceRequestDiscoveryFilter>(), Arg.Any<CancellationToken>());
    }
}
