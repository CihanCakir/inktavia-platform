using System.Reflection;
using Aizen.Bff.Marine.Web.Application.Contracts.ServicePages;
using Aizen.Bff.Marine.Web.Application.ServicePages.Query.GetWebServicePage;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto.ProviderEligibility;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Bff.Marine.Web.UnitTests;

/// <summary>
/// M2 — the BFF service × location aggregation. Resolves serviceSlug → SERVICE_PROVIDER_CATEGORY Code, folds the
/// coarse availability into ONE projection, and never surfaces a provider count/ids. Unknown slug ⇒ clean not-found.
/// </summary>
public sealed class WebServicePageTests
{
    private static FakeReferenceDataRemoteCall Reference() => new()
    {
        LookupsResponse = Env.Ok(new List<LookupItemDto>
        {
            new() { Id = 1, Code = "MOTOR_MAINTENANCE", Name = "Motor Maintenance", IconKey = "motor", SortOrder = 1 },
            new() { Id = 2, Code = "BOAT_CLEANING",     Name = "Boat Cleaning",     SortOrder = 2 },
        }),
    };

    private static GetWebServicePageQueryHandler Handler(FakeReferenceDataRemoteCall reference, FakeIdentityRemoteCall identity)
        => new(reference, identity, NullLogger<GetWebServicePageQueryHandler>.Instance);

    [Fact]
    public async Task Resolves_slug_to_code_and_aggregates_the_coarse_signal()
    {
        var identity = new FakeIdentityRemoteCall(profile: null)
        {
            AvailabilityResponse = Env.Ok(new ProviderAreaAvailabilityDto
            { CityCode = "34", CategoryCode = "MOTOR_MAINTENANCE", Availability = "available" }),
        };

        var page = await Handler(Reference(), identity).Handle(
            new GetWebServicePageQuery { ServiceSlug = "motor-maintenance", CityCode = "34" }, default);

        page.Should().NotBeNull();
        page!.Service.Code.Should().Be("MOTOR_MAINTENANCE");
        page.Service.Slug.Should().Be("motor-maintenance");
        page.Location.CityCode.Should().Be("34");
        page.Availability.Should().Be("available");

        // The module was called with the canonical CODE (not the slug), so its lower(Code) filter matches.
        identity.LastAvailabilityCategory.Should().Be("MOTOR_MAINTENANCE");
        identity.LastAvailabilityCity.Should().Be("34");
    }

    [Fact]
    public async Task Location_slug_form_walks_to_the_city_for_availability()
    {
        var reference = Reference();
        reference.LocationBySlugResponse = new()
        {
            Body = new Aizen.Modules.ReferenceData.Abstraction.Dto.Location.LocationBySlugDto
            {
                LocationType = "district", Code = "KADIKOY", Slug = "istanbul-kadikoy", Name = "Kadıköy",
                ParentChain = new()
                {
                    new() { LocationType = "country", Code = "TR", Name = "Türkiye" },
                    new() { LocationType = "city", Code = "34", Name = "İstanbul" },
                },
            },
        };
        var identity = new FakeIdentityRemoteCall(profile: null)
        {
            AvailabilityResponse = Env.Ok(new ProviderAreaAvailabilityDto
            { CityCode = "34", CategoryCode = "MOTOR_MAINTENANCE", Availability = "limited" }),
        };

        var page = await Handler(reference, identity).Handle(
            new GetWebServicePageQuery { ServiceSlug = "motor-maintenance", LocationSlug = "istanbul-kadikoy" }, default);

        page!.Location.CityCode.Should().Be("34", "a district slug resolves to its parent city for the city-keyed signal");
        page.Availability.Should().Be("limited");
        identity.LastAvailabilityCity.Should().Be("34");
        identity.LastAvailabilityCategory.Should().Be("MOTOR_MAINTENANCE");
    }

    [Fact]
    public async Task Unknown_slug_is_a_clean_not_found_not_faked()
    {
        var identity = new FakeIdentityRemoteCall(profile: null);
        var act = () => Handler(Reference(), identity).Handle(
            new GetWebServicePageQuery { ServiceSlug = "does-not-exist", CityCode = "34" }, default);

        await act.Should().ThrowAsync<AizenBusinessException>();
    }

    [Fact]
    public async Task Availability_defaults_to_none_when_the_signal_is_absent()
    {
        var identity = new FakeIdentityRemoteCall(profile: null);   // AvailabilityResponse null ⇒ empty body
        var page = await Handler(Reference(), identity).Handle(
            new GetWebServicePageQuery { ServiceSlug = "boat-cleaning", CityCode = "34" }, default);

        page!.Availability.Should().Be("none", "never over-state availability when the signal is unavailable");
    }

    [Fact]
    public void Service_page_dto_tree_carries_no_count_or_provider_ids()
    {
        var all = Props<WebServicePageDto>()
            .Concat(Props<WebServicePageLocationDto>())
            .ToArray();

        foreach (var forbidden in new[]
                 {
                     "Count", "Total", "ProfileId", "UserId", "ProviderIds", "Providers", "Ids", "Score", "Rank",
                 })
            all.Should().NotContain(n => n.Contains(forbidden), $"'{forbidden}' must never surface on the service page");

        // Availability is the coarse enum string, nothing structured that could carry a number.
        typeof(WebServicePageDto).GetProperty("Availability")!.PropertyType.Should().Be(typeof(string));
    }

    private static string[] Props<T>() => typeof(T)
        .GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToArray();
}
