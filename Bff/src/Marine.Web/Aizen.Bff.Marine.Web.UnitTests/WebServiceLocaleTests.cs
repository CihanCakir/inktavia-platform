using Aizen.Bff.Marine.Web.Application.Catalogue;
using Aizen.Bff.Marine.Web.Application.Common.Text;
using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupItem;
using FluentAssertions;

namespace Aizen.Bff.Marine.Web.UnitTests;

/// <summary>
/// The locale contract for the service catalogue (D-02): a per-locale slug map, a display name resolved for the
/// requested language, and a reverse lookup that accepts EITHER locale's slug.
///
/// These are regression tests for a real defect, not hypotheticals: the mapper used to drop
/// <c>LookupItemDto.DisplayNameTr</c> entirely — the Turkish name was in the seed and in the DTO the whole time, and
/// the Provider and Mobile BFFs already surfaced it — so a Turkish reader saw "Motor Maintenance" on a Turkish page
/// and browsed to <c>/tr/hizmetler/motor-maintenance</c>.
/// </summary>
public sealed class WebServiceLocaleTests
{
    private static LookupItemDto Motor() => new()
    {
        GroupCode = "SERVICE_PROVIDER_CATEGORY",
        Code = "MOTOR_MAINTENANCE",
        Name = "Motor Maintenance",
        DisplayNameTr = "Motor Bakımı",
        SortOrder = 1,
        IsActive = true,
    };

    /// <summary>No Turkish name authored yet — the honest answer is the English name and NO Turkish slug.</summary>
    private static LookupItemDto Untranslated() => new()
    {
        GroupCode = "SERVICE_PROVIDER_CATEGORY",
        Code = "CONCIERGE_SUPPORT",
        Name = "Concierge Support",
        DisplayNameTr = null,
        SortOrder = 14,
        IsActive = true,
    };

    [Fact]
    public void Turkish_request_gets_the_Turkish_name_and_the_Turkish_slug()
    {
        var s = WebServiceMapper.ToSummary(Motor(), "tr");

        s.Name.Should().Be("Motor Bakımı");
        s.Slug.Should().Be("motor-bakimi", "the dotless ı folds to i — a percent-encoded URL is not a URL anyone can read");
        s.Slugs["tr"].Should().Be("motor-bakimi");
        s.Slugs["en"].Should().Be("motor-maintenance");
    }

    [Fact]
    public void English_request_is_unchanged_from_the_pre_locale_contract()
    {
        var s = WebServiceMapper.ToSummary(Motor(), "en");

        s.Name.Should().Be("Motor Maintenance");
        s.Slug.Should().Be("motor-maintenance");
    }

    [Fact]
    public void Absent_lang_resolves_to_the_default_so_existing_callers_see_no_change()
    {
        WebServiceMapper.ToSummary(Motor(), null).Slug
            .Should().Be(WebServiceMapper.ToSummary(Motor(), "en").Slug);
    }

    [Fact]
    public void English_slug_comes_from_the_code_not_the_name()
    {
        // "Hull & Paint" would fold to hull-paint; the code keeps the URL stable when the marketing copy is reworded.
        var item = new LookupItemDto { Code = "HULL_MAINTENANCE", Name = "Hull & Paint" };
        WebServiceMapper.ToSummary(item, "en").Slug.Should().Be("hull-maintenance");
    }

    [Fact]
    public void An_untranslated_item_omits_the_locale_rather_than_faking_it()
    {
        var s = WebServiceMapper.ToSummary(Untranslated(), "tr");

        s.Slugs.Should().NotContainKey("tr", "an absent key means 'not translated', which the consumer needs to know");
        s.Slugs["en"].Should().Be("concierge-support");
        s.Slug.Should().Be("concierge-support", "falling back to the English slug beats publishing an empty segment");
        s.Name.Should().Be("Concierge Support");
    }

    [Theory]
    [InlineData("motor-bakimi")]
    [InlineData("motor-maintenance")]
    [InlineData("MOTOR-BAKIMI")]
    public void Either_locales_slug_resolves_to_the_same_item(string slug)
        => WebServiceMapper.MatchesSlug(Motor(), slug).Should().BeTrue();

    [Theory]
    [InlineData("motor")]
    [InlineData("")]
    [InlineData("  ")]
    public void An_unknown_slug_does_not_match(string slug)
        => WebServiceMapper.MatchesSlug(Motor(), slug).Should().BeFalse();

    [Theory]
    [InlineData("Motor Bakımı", "motor-bakimi")]
    [InlineData("Tekne Temizliği", "tekne-temizligi")]
    [InlineData("Yiyecek & İçecek", "yiyecek-icecek")]
    [InlineData("Yakıt Desteği", "yakit-destegi")]
    [InlineData("İç Mekan Temizliği", "ic-mekan-temizligi")]
    [InlineData("Kışlama", "kislama")]
    [InlineData("Gövde & Boya", "govde-boya")]
    [InlineData("Döşeme", "doseme")]
    [InlineData("  Fazla   Boşluk  ", "fazla-bosluk")]
    public void Turkish_folding_is_the_map_the_route_contract_specifies(string input, string expected)
        => WebLocale.ToSlug(input).Should().Be(expected);

    [Theory]
    [InlineData("tr", "tr")]
    [InlineData("TR", "tr")]
    [InlineData("tr-TR", "tr")]
    [InlineData("en", "en")]
    [InlineData("de", "en")]
    [InlineData("", "en")]
    [InlineData(null, "en")]
    public void Unsupported_or_missing_lang_falls_back_instead_of_erroring(string? lang, string expected)
        => WebLocale.Normalize(lang).Should().Be(expected);
}
