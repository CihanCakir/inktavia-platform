namespace Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.CargoDry;

/// <summary>
/// Owner-facing CargoDry product (CargoDry supply flow). The app never sees commercial pricing — only retail + media.
/// Media are already resolved to presigned read URLs at the BFF (<see cref="ThumbnailUrl"/>, <see cref="ImageUrls"/>).
/// </summary>
public sealed class MobileCargoDryProductDto
{
    public long    Id             { get; set; }
    public string  ProductCode    { get; set; } = default!;
    public string  Name           { get; set; } = default!;
    public string  Description    { get; set; } = default!;
    public int     ValidityDays   { get; set; }
    public bool    HasSmartDevice { get; set; }
    public string? DeviceType     { get; set; }
    public decimal RetailPrice    { get; set; }
    public string  CurrencyCode   { get; set; } = default!;
    public string? ThumbnailUrl   { get; set; }
    public List<string> ImageUrls { get; set; } = new();
}
