using System.Reflection;
using Aizen.Bff.Marine.Web.Application.Catalogue;
using Aizen.Bff.Marine.Web.Application.Contracts.Catalogue;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using FluentAssertions;

namespace Aizen.Bff.Marine.Web.UnitTests;

/// <summary>
/// W5 — field-stripping for the W4 service-catalogue mapper. The website's taxonomy grid must carry only the public
/// tile fields; the internal lookup plumbing (database id, group wiring, default/active flags) must never surface.
/// </summary>
public sealed class WebServiceMapperTests
{
    private static string[] Props<T>() => typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
        .Select(p => p.Name).ToArray();

    private static LookupItemDto FullItem() => new()
    {
        Id = 4242,                                   // database id — must NOT surface (Code is the public key)
        LookupGroupId = 7,                           // internal — must NOT surface
        GroupCode = "SERVICE_PROVIDER_CATEGORY",
        GroupName = "Service Categories",
        GroupHierarchyPath = "root/services",        // internal — must NOT surface
        Code = "ENGINE_REPAIR",
        Name = "Engine Repair",
        Description = "Diesel & outboard",
        IconKey = "engine",
        ColorCode = "#00aaff",
        SortOrder = 3,
        IsDefault = true,                            // internal — must NOT surface
        IsActive = true,                             // internal — must NOT surface
    };

    [Fact]
    public void ToSummary_maps_public_fields_and_derives_slug()
    {
        var s = WebServiceMapper.ToSummary(FullItem());

        s.Code.Should().Be("ENGINE_REPAIR");
        s.Slug.Should().Be("engine-repair", "the slug is a deterministic transform of the code, not fabricated data");
        s.Name.Should().Be("Engine Repair");
        s.Description.Should().Be("Diesel & outboard");
        s.IconKey.Should().Be("engine");
        s.ColorCode.Should().Be("#00aaff");
        s.SortOrder.Should().Be(3);
    }

    [Theory]
    [InlineData("ENGINE_REPAIR", "engine-repair")]
    [InlineData("HULL & DECK", "hull-deck")]
    [InlineData("  Spaced  Code  ", "spaced-code")]
    [InlineData("ALREADY-OK", "already-ok")]
    public void Slug_is_url_safe_and_deterministic(string code, string expected)
        => WebServiceMapper.ToSummary(new LookupItemDto { Code = code, Name = code }).Slug.Should().Be(expected);

    [Fact]
    public void WebServiceSummaryDto_omits_internal_lookup_fields()
    {
        var names = Props<WebServiceSummaryDto>();
        names.Should().NotContain("Id");
        names.Should().NotContain("LookupGroupId");
        names.Should().NotContain("GroupType");
        names.Should().NotContain("GroupHierarchyPath");
        names.Should().NotContain("GroupName");
        names.Should().NotContain("IsDefault");
        names.Should().NotContain("IsActive");
    }
}
