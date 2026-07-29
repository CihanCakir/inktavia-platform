using Aizen.Modules.ServiceRequest.Application.Query.Offer.GetOfferCommissionPreview;
using FluentAssertions;
using PayEnum = Aizen.Modules.Payment.Abstraction.Enum;
using SrEnum = Aizen.Modules.ServiceRequest.Abstraction.Enum;

namespace Aizen.Modules.ServiceRequest.Application.UnitTests;

public sealed class OfferCommissionPreviewMappingTests
{
    // ── SR item type → Payment LineType dimension ───────────────────────────────

    [Theory]
    [InlineData(SrEnum.ServiceRequestOfferItemType.Service, PayEnum.LineType.Labor)]
    [InlineData(SrEnum.ServiceRequestOfferItemType.Labor, PayEnum.LineType.Labor)]
    [InlineData(SrEnum.ServiceRequestOfferItemType.Installation, PayEnum.LineType.Labor)]
    [InlineData(SrEnum.ServiceRequestOfferItemType.Product, PayEnum.LineType.Part)]
    [InlineData(SrEnum.ServiceRequestOfferItemType.Consumable, PayEnum.LineType.Part)]
    [InlineData(SrEnum.ServiceRequestOfferItemType.Travel, PayEnum.LineType.Travel)]
    [InlineData(SrEnum.ServiceRequestOfferItemType.Delivery, PayEnum.LineType.Travel)]
    [InlineData(SrEnum.ServiceRequestOfferItemType.MarinaOrLiftFee, PayEnum.LineType.PassThrough)]
    [InlineData(SrEnum.ServiceRequestOfferItemType.ExternalService, PayEnum.LineType.PassThrough)]
    public void MapLineType_Maps_By_Economic_Role(SrEnum.ServiceRequestOfferItemType itemType, PayEnum.LineType expected)
        => GetOfferCommissionPreviewQueryHandler.MapLineType(itemType).Should().Be(expected);

    [Fact]
    public void MapLineType_Other_Has_No_Dimension()
        => GetOfferCommissionPreviewQueryHandler.MapLineType(SrEnum.ServiceRequestOfferItemType.Other).Should().BeNull();

    // ── SR line eligibility → Payment line eligibility (1:1) ────────────────────

    [Theory]
    [InlineData(SrEnum.LineCommissionEligibility.Eligible, PayEnum.LineCommissionEligibility.Eligible)]
    [InlineData(SrEnum.LineCommissionEligibility.Exempt, PayEnum.LineCommissionEligibility.Exempt)]
    [InlineData(SrEnum.LineCommissionEligibility.InheritFromCategory, PayEnum.LineCommissionEligibility.InheritFromCategory)]
    public void MapEligibility_Is_One_To_One(SrEnum.LineCommissionEligibility src, PayEnum.LineCommissionEligibility expected)
        => GetOfferCommissionPreviewQueryHandler.MapEligibility(src).Should().Be(expected);
}
