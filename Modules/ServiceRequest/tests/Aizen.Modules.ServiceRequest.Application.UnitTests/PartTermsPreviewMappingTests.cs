using System.Reflection;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;
using Aizen.Modules.ServiceRequest.Application.Query.Offer.GetOfferPartTermsPreview;
using FluentAssertions;
using SrEnum = Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

/// <summary>
/// BE-S5c — the SR part-terms preview maps only Product/Consumable lines, and the response it returns from Payment is the
/// cost-free allowance (no supplier cost / dealer margin ever reaches SR — a static guard mirroring the Payment-side headline test).
/// </summary>
public sealed class PartTermsPreviewMappingTests
{
    [Theory]
    [InlineData(SrEnum.ServiceRequestOfferItemType.Product, true)]
    [InlineData(SrEnum.ServiceRequestOfferItemType.Consumable, true)]
    [InlineData(SrEnum.ServiceRequestOfferItemType.Service, false)]
    [InlineData(SrEnum.ServiceRequestOfferItemType.Labor, false)]
    [InlineData(SrEnum.ServiceRequestOfferItemType.Travel, false)]
    [InlineData(SrEnum.ServiceRequestOfferItemType.MarinaOrLiftFee, false)]
    public void IsPartLineType_Only_Product_And_Consumable(SrEnum.ServiceRequestOfferItemType itemType, bool expected)
        => GetOfferPartTermsPreviewQueryHandler.IsPartLineType(itemType).Should().Be(expected);

    [Fact]
    public void PartTerms_Response_Type_Is_Cost_Free()
    {
        // The SR handler returns exactly this Payment response type; assert it carries no cost/margin field.
        var forbidden = new[] { "listprice", "dealermargin", "cost", "margin" };
        foreach (var type in new[] { typeof(ResolvePartLineAllowancesRemoteCallResponse), typeof(PartLineAllowanceDto), typeof(PartFundingSplitDto) })
            foreach (var p in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                forbidden.Any(f => p.Name.ToLowerInvariant().Contains(f))
                    .Should().BeFalse($"{type.Name}.{p.Name} would leak part cost/margin to ServiceRequest");
    }
}
