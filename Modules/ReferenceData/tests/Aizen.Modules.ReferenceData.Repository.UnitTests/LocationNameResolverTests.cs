using Aizen.Modules.ReferenceData.Abstraction.Localization;
using FluentAssertions;

namespace Aizen.Modules.ReferenceData.Repository.UnitTests;

// FAZ12B #49 — konum adının çağıranın diline göre çözülmesi.
public class LocationNameResolverTests
{
    private static readonly Dictionary<string, string> Istanbul = new()
    {
        ["tr"] = "İstanbul",
        ["en"] = "Istanbul",
    };

    [Fact]
    public void Accept_language_header_resolves_to_Turkish()
    {
        LocationNameResolver.Resolve(Istanbul, "34", "tr-TR,tr;q=0.9,en;q=0.8").Should().Be("İstanbul");
    }

    [Fact]
    public void English_locale_resolves_to_English()
    {
        LocationNameResolver.Resolve(Istanbul, "34", "en-US").Should().Be("Istanbul");
    }

    [Fact]
    public void Language_with_no_translation_falls_back_to_en()
    {
        // fr istenmiş ama sözlükte yok → "en" yedeği.
        LocationNameResolver.Resolve(Istanbul, "34", "fr-FR,fr;q=0.9").Should().Be("Istanbul");
    }

    [Fact]
    public void No_accessor_null_header_falls_back_to_en()
    {
        LocationNameResolver.Resolve(Istanbul, "34", null).Should().Be("Istanbul");
    }

    [Fact]
    public void Malformed_header_falls_back_to_en_not_throw()
    {
        LocationNameResolver.Resolve(Istanbul, "34", ";;;q=,,").Should().Be("Istanbul");
    }

    [Fact]
    public void Highest_q_weight_wins_regardless_of_order()
    {
        // en önce ama tr daha yüksek q → tr.
        LocationNameResolver.PrimaryLanguageTag("en;q=0.8,tr;q=0.9").Should().Be("tr");
    }

    [Theory]
    [InlineData("tr-TR,tr;q=0.9,en;q=0.8", "tr")]
    [InlineData("en-US", "en")]
    [InlineData("TR", "tr")]
    [InlineData("*", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void PrimaryLanguageTag_parses_header(string? header, string? expected)
    {
        LocationNameResolver.PrimaryLanguageTag(header).Should().Be(expected);
    }

    [Fact]
    public void Falls_back_to_any_value_then_code_when_no_tr_or_en()
    {
        var onlyDe = new Dictionary<string, string> { ["de"] = "Istanbul-DE" };
        LocationNameResolver.Resolve(onlyDe, "34", "fr").Should().Be("Istanbul-DE");
        LocationNameResolver.Resolve(new Dictionary<string, string>(), "34", "tr").Should().Be("34");
    }
}
