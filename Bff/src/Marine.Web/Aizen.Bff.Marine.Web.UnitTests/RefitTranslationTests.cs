using System.Net;
using Aizen.Bff.Marine.Web.Application.Content.Query.GetWebContentBySlug;
using Aizen.Bff.Marine.Web.Application.Reference.Query.GetWebCitiesByCountry;
using Aizen.Core.Infrastructure.Exception;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Bff.Marine.Web.UnitTests;

/// <summary>
/// W4 — uniform Refit <c>ApiException</c> → clean <see cref="AizenBusinessException"/> translation: a 404 maps to a
/// specific "not found" message; any other non-2xx maps to a generic "unavailable" message. No Refit type or raw
/// status leaks to the caller.
/// </summary>
public sealed class RefitTranslationTests
{
    [Fact]
    public async Task BySlug_404_maps_to_specific_not_found()
    {
        var content = new FakeContentRemoteCall { ThrowOnBySlug = await RefitFault.Of(HttpStatusCode.NotFound) };
        var handler = new GetWebContentBySlugQueryHandler(content, new FakeSeoIndexabilityPolicy(), NullLogger<GetWebContentBySlugQueryHandler>.Instance);

        var act = () => handler.Handle(new GetWebContentBySlugQuery { Slug = "missing", Lang = "tr" }, default);

        (await act.Should().ThrowAsync<AizenBusinessException>()).Which.Message.Should().Be("Content not found.");
    }

    [Fact]
    public async Task BySlug_500_maps_to_generic_unavailable()
    {
        var content = new FakeContentRemoteCall { ThrowOnBySlug = await RefitFault.Of(HttpStatusCode.InternalServerError) };
        var handler = new GetWebContentBySlugQueryHandler(content, new FakeSeoIndexabilityPolicy(), NullLogger<GetWebContentBySlugQueryHandler>.Instance);

        var act = () => handler.Handle(new GetWebContentBySlugQuery { Slug = "s", Lang = "tr" }, default);

        (await act.Should().ThrowAsync<AizenBusinessException>()).Which.Message.Should().Contain("unavailable");
    }

    [Fact]
    public async Task Cities_404_maps_to_country_not_found()
    {
        var reference = new FakeReferenceDataRemoteCall { ThrowOnCities = await RefitFault.Of(HttpStatusCode.NotFound) };
        var handler = new GetWebCitiesByCountryQueryHandler(reference, NullLogger<GetWebCitiesByCountryQueryHandler>.Instance);

        var act = () => handler.Handle(new GetWebCitiesByCountryQuery { CountryCode = "ZZ" }, default);

        (await act.Should().ThrowAsync<AizenBusinessException>()).Which.Message.Should().Contain("not found");
    }

    [Fact]
    public async Task Cities_502_maps_to_generic_unavailable()
    {
        var reference = new FakeReferenceDataRemoteCall { ThrowOnCities = await RefitFault.Of(HttpStatusCode.BadGateway) };
        var handler = new GetWebCitiesByCountryQueryHandler(reference, NullLogger<GetWebCitiesByCountryQueryHandler>.Instance);

        var act = () => handler.Handle(new GetWebCitiesByCountryQuery { CountryCode = "TR" }, default);

        var ex = (await act.Should().ThrowAsync<AizenBusinessException>()).Which;
        ex.Message.Should().Contain("unavailable");
        ex.Message.Should().NotContain("not found");
    }
}
