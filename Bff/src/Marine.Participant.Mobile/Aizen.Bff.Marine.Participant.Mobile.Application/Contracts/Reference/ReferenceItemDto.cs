namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Reference;

/// <summary>A single lookup option surfaced to the mobile client (dropdowns). Trimmed from the module LookupItemDto.</summary>
public sealed class ReferenceItemDto
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    /// <summary>Turkish display name (additive; null → client falls back to <see cref="Name"/>). Client picks by language.</summary>
    public string? DisplayNameTr { get; set; }
    public string? Description { get; set; }
    public string? IconKey { get; set; }
    public int SortOrder { get; set; }
    public bool IsDefault { get; set; }
}

/// <summary>A country option (dropdowns / phone prefix). `DialCode` is the ITU phone code.</summary>
public sealed class CountryItemDto
{
    public string Code { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? DialCode { get; set; }
    public string? CurrencyCode { get; set; }
}
