using Aizen.Modules.Payment.Application.Services;
using FluentAssertions;

namespace Aizen.Modules.Payment.Domain.UnitTests;

/// <summary>
/// CargoDry supply flow — guard (a): a CARGODRY_SUPPLY acceptance is a PrincipalSale and must NEVER declare a nonzero
/// provider split. The acceptance path builds the escrow with <c>PlatformCollectedNoProviderShare = true</c>, which
/// routes commission resolution through <see cref="CommissionBreakdown.PlatformOnly"/>. This pins that a
/// platform-only breakdown always yields Commission = 0 and NetPayout(provider) = 0, so the whole gross is platform
/// revenue and no provider is ever paid out of this escrow (the provider is paid via the CargoDry sell-through settlement).
/// </summary>
public sealed class CargoDrySupplyPlatformOnlyEscrowTests
{
    [Theory]
    [InlineData(149.99, 0)]
    [InlineData(1000, 0)]
    [InlineData(250.50, 50.50)]
    public void PlatformOnly_breakdown_declares_zero_provider_split(decimal gross, decimal discount)
    {
        var breakdown = CommissionBreakdown.PlatformOnly(gross, discount);

        breakdown.CommissionAmount.Should().Be(0m, "PrincipalSale has no provider commission");
        breakdown.CommissionRate.Should().Be(0m);
        breakdown.VatOnCommission.Should().Be(0m);
        breakdown.NetPayoutAmount.Should().Be(0m, "no provider is paid out of a CARGODRY_SUPPLY escrow");
        breakdown.GrossAmount.Should().Be(gross, "the whole gross is platform revenue");
        breakdown.DiscountAmount.Should().Be(discount);
    }
}
