using Aizen.Core.Domain;
using Aizen.Modules.CargoDry.Abstraction.Model;

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
    public decimal RetailPrice    { get; private set; }
    public string  CurrencyCode   { get; private set; } = default!;

    // IsActive inherited from AizenEntityWithAudit — do NOT redeclare
    // CreateDate inherited from AizenEntityWithAudit — do NOT redeclare

    private CargoDryProductEntity() { }

    public static CargoDryProductEntity Create(
        string productCode, string name, string description,
        int validityDays, decimal retailPrice, string currencyCode,
        bool hasSmartDevice = false)
        => new()
        {
            ProductCode    = productCode.ToUpperInvariant(),
            Name           = name,
            Description    = description,
            ValidityDays   = validityDays,
            RetailPrice    = retailPrice,
            CurrencyCode   = currencyCode.ToUpperInvariant(),
            HasSmartDevice = hasSmartDevice,
            IsActive       = true,
        };

    public void SetActive(bool active) => IsActive = active;
    public void UpdatePrice(decimal price) => RetailPrice = price;
}
