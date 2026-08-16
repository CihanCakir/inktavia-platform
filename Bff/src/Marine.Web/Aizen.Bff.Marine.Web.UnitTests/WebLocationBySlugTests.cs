using Aizen.Bff.Marine.Web.Application.Location.Query.GetWebLocationBySlug;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace Aizen.Bff.Marine.Web.UnitTests;

/// <summary>
/// M3 — the collapsed <c>web/locations/{slug}</c> handler resolves via the ReferenceData flat resolver and maps to the
/// web location shape (identity + parent chain). Unknown slug ⇒ clean not-found.
/// </summary>
public sealed class WebLocationBySlugTests
{
    private static GetWebLocationBySlugQueryHandler Handler(FakeReferenceDataRemoteCall reference)
        => new(reference, NullLogger<GetWebLocationBySlugQueryHandler>.Instance);

    [Fact]
    public async Task Resolves_slug_and_maps_parent_chain()
    {
        var reference = new FakeReferenceDataRemoteCall
        {
            LocationBySlugResponse = new()
            {
                Body = new LocationBySlugDto
                {
                    LocationType = "district", Code = "KADIKOY", Name = "Kadıköy", Slug = "istanbul-kadikoy",
                    ParentChain = new()
                    {
                        new LocationRefDto { LocationType = "country", Code = "TR", Name = "Türkiye" },
                        new LocationRefDto { LocationType = "city", Code = "34", Name = "İstanbul" },
                    },
                },
            },
        };

        var dto = await Handler(reference).Handle(new GetWebLocationBySlugQuery { Slug = "istanbul-kadikoy" }, default);

        dto!.LocationType.Should().Be("district");
        dto.Code.Should().Be("KADIKOY");
        dto.Name.Should().Be("Kadıköy");
        dto.ParentChain.Select(p => (p.LocationType, p.Code, p.Name))
            .Should().Equal(("country", "TR", "Türkiye"), ("city", "34", "İstanbul"));
        reference.LastBySlug.Should().Be("istanbul-kadikoy");
    }

    [Fact]
    public async Task Unknown_slug_is_a_clean_not_found()
    {
        var reference = new FakeReferenceDataRemoteCall();   // null-body response
        var act = () => Handler(reference).Handle(new GetWebLocationBySlugQuery { Slug = "nope" }, default);
        await act.Should().ThrowAsync<AizenBusinessException>();
    }
}
