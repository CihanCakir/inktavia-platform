namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Responses;

/// <summary>
/// BE-S5c — response: the derived, <b>cost-free</b> part-line allowance per input line. This is the ONLY projection of a
/// <c>PartCommercialTerm</c> that leaves the Payment module. It deliberately carries <b>no</b> <c>supplierListPrice</c>, no
/// <c>providerDealerMargin</c> and no raw cost — only the caps a downstream (SR/FE, and later S9) may act on. A confidentiality
/// unit test asserts these fields are absent (and fails if anyone adds a cost field to <see cref="PartLineAllowanceDto"/>).
/// </summary>
public sealed class ResolvePartLineAllowancesRemoteCallResponse
{
    public required List<PartLineAllowanceDto> Lines { get; init; } = new();
    public string CurrencyCode { get; init; } = "TRY";
}

/// <summary>
/// BE-S5b — the cost-free allowance for one part line. <see cref="Found"/> is false when no term resolved (the numeric fields
/// are then 0). NO supplier cost / dealer margin — if a value could be back-solved into the cost, it does not belong here.
/// </summary>
public sealed class PartLineAllowanceDto
{
    /// <summary>Correlation key = the offer-item id echoed from the request.</summary>
    public required string LineRef { get; init; }
    public bool            Found   { get; init; }
    public string?         ProductCode { get; init; }

    /// <summary>The maximum customer discount allowed on this part line.</summary>
    public decimal MaxAllowedCustomerDiscount { get; init; }

    /// <summary>The funded split (who funds how much of an <i>applied</i> discount). Cost-free amounts.</summary>
    public required PartFundingSplitDto Funding { get; init; }

    /// <summary>The floor the provider must still receive on this line.</summary>
    public decimal MinimumProviderReceivable { get; init; }
}

/// <summary>BE-S5b — the funded split of an applied part discount (supplier / provider / platform). Cost-free.</summary>
public sealed class PartFundingSplitDto
{
    public decimal SupplierFundedAmount { get; init; }
    public decimal ProviderFundedAmount { get; init; }
    public decimal PlatformFundedAmount { get; init; }
}
