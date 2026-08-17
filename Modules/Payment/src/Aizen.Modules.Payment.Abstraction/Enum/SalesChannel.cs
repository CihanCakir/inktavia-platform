namespace Aizen.Modules.Payment.Abstraction.Enum;

/// <summary>
/// Payment-module copy of the CargoDry SalesChannel enum.
/// Used in CommissionRuleEntity to filter commission rules by the channel through which
/// a CargoDry kit was sold, without creating a cross-module dependency.
///
/// Integer values are identical to CargoDry.Abstraction.Enum.SalesChannel.
/// </summary>
public enum SalesChannel
{
    /// <summary>Inktavia sells directly through the platform. No provider commission.</summary>
    DirectSale           = 1,

    /// <summary>Provider purchased batch upfront at wholesale price.</summary>
    ProviderResale       = 2,

    /// <summary>Provider holds stock on consignment; revenue on activation.</summary>
    ConsignmentSellThrough = 3,

    /// <summary>Buyer pays Inktavia; provider assisted and receives commission.</summary>
    ProviderAttributedSale = 4,
}
