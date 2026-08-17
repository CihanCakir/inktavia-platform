using System.Globalization;
using Aizen.Modules.Payment.Domain.Money;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests.Economics;

public sealed class MoneyMathTests
{
    private static decimal D(string s) => decimal.Parse(s, CultureInfo.InvariantCulture);

    [Theory]
    [InlineData("1.005", "1.01")]   // midpoint → away-from-zero (banker's rounding would give 1.00)
    [InlineData("2.675", "2.68")]   // classic binary-float trap; decimal + AwayFromZero is exact
    [InlineData("-1.005", "-1.01")] // away-from-zero is symmetric
    [InlineData("100.000", "100.00")]
    [InlineData("0.014", "0.01")]
    public void Round_Uses_TwoDecimals_AwayFromZero(string input, string expected)
    {
        MoneyMath.Round(D(input)).Should().Be(D(expected));
    }

    [Theory]
    [InlineData("0.12345", "0.1235")]   // 4-dp away-from-zero
    [InlineData("0.02500", "0.0250")]
    [InlineData("0.15", "0.1500")]
    public void RoundRate_Uses_FourDecimals_AwayFromZero(string input, string expected)
    {
        MoneyMath.RoundRate(D(input)).Should().Be(D(expected));
    }
}
