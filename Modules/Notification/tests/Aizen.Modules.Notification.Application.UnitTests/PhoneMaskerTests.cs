using Aizen.Modules.Notification.Application.Services;
using FluentAssertions;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>Faz 28.8 — telefon maskeleme (KVKK): ilk 5 + son 2 görünür, orta maskeli; kısa/boş güvenli.</summary>
public sealed class PhoneMaskerTests
{
    [Theory]
    [InlineData("+905551234567", "+9055*****67")]
    [InlineData("+14155552671", "+1415*****71")]
    [InlineData("05551234567", "05551*****67")]
    public void Masks_long_numbers(string input, string expected)
        => PhoneMasker.Mask(input).Should().Be(expected);

    [Theory]
    [InlineData(null, "(yok)")]
    [InlineData("", "(yok)")]
    [InlineData("   ", "(yok)")]
    public void Empty_or_null_returns_placeholder(string? input, string expected)
        => PhoneMasker.Mask(input).Should().Be(expected);

    [Theory]
    [InlineData("12345", "***45")]   // kısa: son 2 görünür
    [InlineData("12", "**")]          // çok kısa: tamamen maskeli
    public void Short_numbers_reveal_at_most_last_two(string input, string expected)
        => PhoneMasker.Mask(input).Should().Be(expected);

    [Fact]
    public void Full_number_never_appears_in_mask()
        => PhoneMasker.Mask("+905551234567").Should().NotContain("5551234");
}
