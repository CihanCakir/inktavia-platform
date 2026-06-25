using Aizen.Core.Domain;

namespace Aizen.Modules.CargoDry.Domain.Entities;

public sealed class CargoDryProductEntity : AizenEntity
{
    public string  ProductCode   { get; private set; } = default!;
    public string  Name          { get; private set; } = default!;
    public string  Description   { get; private set; } = default!;
    public int     ValidityDays  { get; private set; }
    public bool    HasSmartDevice{ get; private set; }
    public decimal RetailPrice   { get; private set; }
    public string  CurrencyCode  { get; private set; } = default!;
    public bool    IsActive      { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

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
            CreatedAt      = DateTimeOffset.UtcNow,
        };

    public void SetActive(bool active) => IsActive = active;
    public void UpdatePrice(decimal price) => RetailPrice = price;
}
