using Aizen.Core.Domain;

namespace Aizen.Modules.Payment.Domain.Entities.Economics;

/// <summary>
/// S2d (§20.15/§20.6) — immutable, insert-only snapshot of ONE pricing attribute value on an offer line, captured at
/// acceptance as a child of <see cref="OfferLineEconomicsSnapshotEntity"/> (FK, OnDelete Restrict). Fills the S8-reserved
/// attribute slot. Written once by <see cref="PaymentEconomicsSnapshotEntity.CreateFromLines"/>; never mutated (private
/// setters, no methods; the only construction path is the validating <see cref="Create"/> factory). <b>Descriptive
/// metadata only</b> — it is not part of the line money math nor any of the 8 equalities.
///
/// <para>Self-contained (§20.15): for a Lookup attribute both <see cref="ValueLookupItemCode"/> and its denormalized
/// <see cref="ValueLookupItemLabel"/> are stored, so reading the snapshot after acceptance needs <b>no</b> re-lookup.
/// <see cref="DataType"/> is the raw SR <c>PricingAttributeDataType</c> int (Payment holds it opaquely — no cross-module
/// enum dependency), mirroring how the line snapshot stores ItemType/PricingMethod.</para>
/// </summary>
[DocumentationInfo("Offer line attribute snapshot entity",
    "Immutable per-attribute child of OfferLineEconomicsSnapshot (S2d). Definition code + resolved value + (Lookup) item " +
    "code and denormalized display label. Descriptive — not in the money math.")]
public sealed class OfferLineAttributeSnapshotEntity : AizenEntityWithAudit
{
    public long     OfferLineEconomicsSnapshotId { get; private set; }
    public string   DefinitionCode               { get; private set; } = default!;
    public int      DataType                     { get; private set; }   // raw SR PricingAttributeDataType value
    public string?  ValueLookupItemCode          { get; private set; }
    public string?  ValueLookupItemLabel         { get; private set; }   // denormalized — self-contained, no re-lookup
    public decimal? ValueNumber                  { get; private set; }
    public string?  ValueText                    { get; private set; }
    public bool?    ValueBool                    { get; private set; }
    public int      SortOrder                    { get; private set; }

    private OfferLineAttributeSnapshotEntity() { }

    public static OfferLineAttributeSnapshotEntity Create(
        string definitionCode, int dataType,
        string? valueLookupItemCode, string? valueLookupItemLabel,
        decimal? valueNumber, string? valueText, bool? valueBool, int sortOrder)
    {
        if (string.IsNullOrWhiteSpace(definitionCode))
            throw new PaymentEconomicsInvariantException("OfferLineAttributeSnapshot requires a DefinitionCode.");

        return new OfferLineAttributeSnapshotEntity
        {
            DefinitionCode       = definitionCode.Trim().ToUpperInvariant(),
            DataType             = dataType,
            ValueLookupItemCode  = string.IsNullOrWhiteSpace(valueLookupItemCode) ? null : valueLookupItemCode.Trim().ToUpperInvariant(),
            ValueLookupItemLabel = string.IsNullOrWhiteSpace(valueLookupItemLabel) ? null : valueLookupItemLabel.Trim(),
            ValueNumber          = valueNumber,
            ValueText            = string.IsNullOrWhiteSpace(valueText) ? null : valueText.Trim(),
            ValueBool            = valueBool,
            SortOrder            = sortOrder,
            IsActive             = true,
        };
    }
}
