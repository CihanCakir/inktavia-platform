namespace Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;

/// <summary>
/// BE-S5c — request: ServiceRequest → Payment to resolve the cost-free part-line allowance for an offer's Product/Consumable
/// lines. Read-only / idempotent (pure resolution — no state written). The response carries <b>no</b> supplier cost or dealer
/// margin (see <c>PartLineAllowanceDto</c>). Typed DTOs (never <c>object</c>).
/// </summary>
public sealed class ResolvePartLineAllowancesRemoteCallRequest
{
    public long?           ProviderProfileId { get; init; }   // shared across all lines (offer provider)
    public required string CurrencyCode      { get; init; }
    /// <summary>Point-in-time for effective-date resolution (UTC). Null → resolve as-of now.</summary>
    public DateTime?       AsOfUtc           { get; init; }

    public required List<ResolvePartLineInputDto> Lines { get; init; } = new();
}

/// <summary>A single part line to resolve (BE-S5c). ProductCode/Brand may be null (then only provider/category/global terms match).</summary>
public sealed class ResolvePartLineInputDto
{
    /// <summary>Offer-item id / correlation key echoed back on the result.</summary>
    public required string LineRef      { get; init; }
    public string?         ProductCode  { get; init; }
    public string?         Brand        { get; init; }
    public string?         CategoryCode { get; init; }
}
