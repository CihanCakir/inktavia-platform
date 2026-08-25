using Aizen.Modules.Notification.Application.Services;
using FluentAssertions;

namespace Aizen.Modules.Notification.Application.UnitTests;

/// <summary>Faz 28.7 — SMS segment sınır testleri: GSM-7 160/153, UCS-2 70/67, emoji, Türkçe ş/ğ/ı → UCS-2, extension = 2 septet.</summary>
public sealed class SmsSegmentCalculatorTests
{
    [Fact]
    public void Gsm7_160_is_one_segment()
    {
        var r = SmsSegmentCalculator.Calculate(new string('a', 160));
        r.Encoding.Should().Be("GSM-7");
        r.Length.Should().Be(160);
        r.Segments.Should().Be(1);
    }

    [Fact]
    public void Gsm7_161_is_two_segments()
    {
        var r = SmsSegmentCalculator.Calculate(new string('a', 161));
        r.Encoding.Should().Be("GSM-7");
        r.Segments.Should().Be(2);
    }

    [Fact]
    public void Ucs2_70_is_one_segment()
    {
        var r = SmsSegmentCalculator.Calculate(new string('ş', 70));   // ş GSM-7'de yok → UCS-2
        r.Encoding.Should().Be("UCS-2");
        r.Length.Should().Be(70);
        r.Segments.Should().Be(1);
    }

    [Fact]
    public void Ucs2_71_is_two_segments()
    {
        var r = SmsSegmentCalculator.Calculate(new string('ş', 71));
        r.Encoding.Should().Be("UCS-2");
        r.Segments.Should().Be(2);
    }

    [Theory]
    [InlineData('ş')]
    [InlineData('ğ')]
    [InlineData('ı')]
    public void Turkish_specific_chars_force_ucs2(char turkish)
        => SmsSegmentCalculator.Calculate(turkish.ToString()).Encoding.Should().Be("UCS-2");

    [Fact]
    public void Emoji_is_ucs2_and_counts_surrogate_pair_as_two_units()
    {
        var r = SmsSegmentCalculator.Calculate("😀");   // astral → 2 UTF-16 birimi
        r.Encoding.Should().Be("UCS-2");
        r.Length.Should().Be(2);
        r.Segments.Should().Be(1);
    }

    [Fact]
    public void Extension_char_counts_as_two_septets()
    {
        var r = SmsSegmentCalculator.Calculate("€");   // GSM-7 extension
        r.Encoding.Should().Be("GSM-7");
        r.Length.Should().Be(2);
        r.Segments.Should().Be(1);
    }

    [Fact]
    public void Gsm7_default_alphabet_turkish_chars_stay_gsm7()
    {
        // ö/ü (ve büyük Ç/Ö/Ü) GSM-7 varsayılan alfabede vardır → GSM-7 kalır. (ş/ğ/ı ve KÜÇÜK ç alfabede yok → UCS-2.)
        SmsSegmentCalculator.Calculate("Merhaba öü ÇÖÜ").Encoding.Should().Be("GSM-7");
        // Kontrast: küçük ç varsayılan GSM-7 alfabesinde YOKTUR (yalnız büyük Ç var) → UCS-2.
        SmsSegmentCalculator.Calculate("naçizane").Encoding.Should().Be("UCS-2");
    }
}
