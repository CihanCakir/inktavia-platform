using System.Linq;
using Aizen.Core.Common.Abstraction.Exception;
using Aizen.Core.Common.Abstraction.Localization;
using Aizen.Core.Infrastructure.Exception;
using FluentAssertions;

namespace Aizen.Modules.ReferenceData.Repository.UnitTests;

// FAZ13A #68 — UÇTAN UCA: Accept-Language başlığı → ErrorLanguageResolver → AizenException.GetErrorMessage,
// gerçek sözlük mekanizmasıyla (izole gömülü test kaynağı). BuilderMiddleware bu iki parçayı aynen bağlar.
// Bir test sözlüğü kullanıyoruz ki modülün gömülü kaynağına bağımlı olmayalım (ConfigureResource global'dir;
// bu assembly'de GetErrorMessage kullanan başka test yok, sınıf içi metotlar da sıralı çalışır).
[Collection("ErrorLocalization")]
public class ErrorMessageLocalizationTests
{
    private const int TestCode = 99001;

    public ErrorMessageLocalizationTests()
    {
        var asm = typeof(ErrorMessageLocalizationTests).Assembly;
        var res = asm.GetManifestResourceNames().First(n => n.EndsWith("test_error_messages.json"));
        AizenException.ConfigureResource(asm, res);
    }

    private static AizenException Coded() => new AizenBusinessException(TestCode, "fallback-english");

    [Fact]
    public void Accept_language_tr_produces_Turkish_message()
    {
        var lang = ErrorLanguageResolver.Resolve("tr-TR,tr;q=0.9,en;q=0.8", null);
        Coded().GetErrorMessage(lang).Should().Be("Türkçe mesaj");
    }

    [Fact]
    public void Accept_language_en_produces_English_message()
    {
        var lang = ErrorLanguageResolver.Resolve("en-US", null);
        Coded().GetErrorMessage(lang).Should().Be("English message");
    }

    [Fact]
    public void Unknown_language_falls_back_to_existing_behaviour_not_empty()
    {
        var lang = ErrorLanguageResolver.Resolve("fr-FR,fr;q=0.9", null); // "fr" — sözlükte yok
        var msg = Coded().GetErrorMessage(lang);
        // Boş string DEĞİL; sözlüğün ilk (TR) girdisine düşer.
        msg.Should().NotBeNullOrWhiteSpace();
        msg.Should().Be("Türkçe mesaj");
    }

    [Fact]
    public void No_header_defaults_to_TR()
    {
        var lang = ErrorLanguageResolver.Resolve(null, null);
        lang.Should().Be("TR");
        Coded().GetErrorMessage(lang).Should().Be("Türkçe mesaj");
    }
}
