using Aizen.Core.Domain;

namespace Aizen.Modules.CargoDry.Domain.Entities;

[DocumentationInfo("CargoDry Product entity",
    "Defines a moisture protection kit product variant (e.g., Standard-90, Premium-180). " +
    "Product catalog is managed by admin. ValidityDays drives kit expiry calculation on activation.")]
public sealed class CargoDryProductEntity : AizenEntityWithAudit
{
    public string  ProductCode    { get; private set; } = default!;
    public string  Name           { get; private set; } = default!;
    public string  Description    { get; private set; } = default!;
    public int     ValidityDays   { get; private set; }
    public bool    HasSmartDevice { get; private set; }
    /// <summary>Optional device variant identifier (e.g. "AQUASENSE-V5"). Required when HasSmartDevice is true.</summary>
    public string? DeviceType     { get; private set; }
    public decimal RetailPrice    { get; private set; }
    public string  CurrencyCode   { get; private set; } = default!;

    // IsActive inherited from AizenEntityWithAudit — do NOT redeclare
    // CreateDate inherited from AizenEntityWithAudit — do NOT redeclare

    private CargoDryProductEntity() { }

    public static CargoDryProductEntity Create(
        string productCode, string name, string description,
        int validityDays, decimal retailPrice, string currencyCode,
        bool hasSmartDevice = false, string? deviceType = null)
        => new()
        {
            ProductCode    = productCode.ToUpperInvariant(),
            Name           = name,
            Description    = description,
            ValidityDays   = validityDays,
            RetailPrice    = retailPrice,
            CurrencyCode   = currencyCode.ToUpperInvariant(),
            HasSmartDevice = hasSmartDevice,
            DeviceType     = deviceType,
            IsActive       = true,
        };

    public void SetActive(bool active) => IsActive = active;
    public void Activate()   => IsActive = true;
    public void Deactivate() => IsActive = false;
    public void UpdatePrice(decimal price) => RetailPrice = price;

    public void Update(
        string name, string description, int validityDays,
        decimal retailPrice, string currencyCode,
        bool hasSmartDevice, string? deviceType)
    {
        Name           = name;
        Description    = description;
        ValidityDays   = validityDays;
        RetailPrice    = retailPrice;
        CurrencyCode   = currencyCode.ToUpperInvariant();
        HasSmartDevice = hasSmartDevice;
        DeviceType     = deviceType;
    }
}
