using Aizen.Modules.Notification.Application.Services;
using FluentAssertions;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>Faz 28.7 — E.164 normalizasyonu tablo testi (DefaultCountryCode "+90").</summary>
public sealed class PhoneNumberNormalizerTests
{
    [Theory]
    [InlineData("+90 555 123 45 67", "+905551234567")]   // +'lı, ayraçlı
    [InlineData("+90 (555) 123-4567", "+905551234567")]  // parantez/tire
    [InlineData("0555 123 4567", "+905551234567")]       // baştaki 0'lı ulusal → ülke kodu
    [InlineData("0 (555) 123-4567", "+905551234567")]
    [InlineData("00905551234567", "+905551234567")]      // 00 uluslararası öneki → +
    [InlineData("905551234567", "+905551234567")]        // +'sız, 0'sız → zaten ülke kodlu varsayılır
    [InlineData("+905551234567", "+905551234567")]       // zaten E.164
    [InlineData("5551234567", "+5551234567")]            // 0'sız → aynen '+' eklenir (dokümante davranış)
    public void ToE164_normalizes(string raw, string expected)
        => PhoneNumberNormalizer.ToE164(raw, "+90").Should().Be(expected);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]        // rakam yok
    public void ToE164_returns_null_when_no_digits(string? raw)
        => PhoneNumberNormalizer.ToE164(raw, "+90").Should().BeNull();

    [Fact]
    public void DefaultCountryCode_without_plus_still_yields_e164()
        => PhoneNumberNormalizer.ToE164("05551234567", "90").Should().Be("+905551234567");
}
