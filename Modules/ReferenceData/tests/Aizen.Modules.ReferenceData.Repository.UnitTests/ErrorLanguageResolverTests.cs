using Aizen.Core.Common.Abstraction.Localization;
using FluentAssertions;

namespace Aizen.Modules.ReferenceData.Repository.UnitTests;

// FAZ13A #68 — hata mesajı dili çözümleme sırası: Accept-Language → legacy "Language" → "TR".
public class ErrorLanguageResolverTests
{
    [Fact]
    public void Accept_language_header_wins_and_is_parsed_to_primary_tag()
    {
        ErrorLanguageResolver.Resolve("tr-TR,tr;q=0.9,en;q=0.8", null).Should().Be("tr");
        ErrorLanguageResolver.Resolve("en-US", null).Should().Be("en");
    }

    [Fact]
    public void Unknown_language_still_resolves_dictionary_fallback_is_GetErrorMessages_job()
    {
        // Çözümleyici dili FİLTRELEMEZ; sözlük yedeği GetErrorMessage'ın işi.
        ErrorLanguageResolver.Resolve("fr-FR,fr;q=0.9", null).Should().Be("fr");
    }

    [Fact]
    public void No_header_at_all_defaults_to_TR_exactly_as_before()
    {
        ErrorLanguageResolver.Resolve(null, null).Should().Be("TR");
        ErrorLanguageResolver.Resolve("", "").Should().Be("TR");
    }

    [Fact]
    public void Malformed_accept_language_falls_through_to_TR_not_throw()
    {
        ErrorLanguageResolver.Resolve(";;;q=,,", null).Should().Be("TR");
        ErrorLanguageResolver.Resolve("*", null).Should().Be("TR");
    }

    [Fact]
    public void Legacy_language_header_still_honoured_when_no_accept_language()
    {
        ErrorLanguageResolver.Resolve(null, "EN").Should().Be("EN");
        // Accept-Language legacy'yi geçer.
        ErrorLanguageResolver.Resolve("tr-TR", "EN").Should().Be("tr");
    }
}
