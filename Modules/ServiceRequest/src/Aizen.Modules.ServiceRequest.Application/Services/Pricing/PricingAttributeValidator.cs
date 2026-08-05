using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;
using Aizen.Modules.ServiceRequest.Domain.Entities.Pricing;
using Aizen.Modules.ServiceRequest.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ServiceRequest.Application.Services.Pricing;

/// <summary>
/// S2b — validates the pricing-attribute values a provider set on an offer line against the definitions applicable to the
/// SR's <c>ServiceCategoryCode</c>. Loads the definitions + resolves each Lookup group's members (via the R4 remote call),
/// then delegates to the pure <see cref="PricingAttributeValueValidation"/> core. Descriptive metadata — never touches the
/// line money math.
/// </summary>
public sealed class PricingAttributeValidator
{
    private readonly ServiceRequestDbContext _db;
    private readonly ReferenceDataLookupClient _lookup;

    public PricingAttributeValidator(ServiceRequestDbContext db, ReferenceDataLookupClient lookup)
    {
        _db = db;
        _lookup = lookup;
    }

    /// <summary>Returns the active definitions applicable to the given category (used by validation + FE pickers).</summary>
    public async Task<List<PricingAttributeDefinitionEntity>> GetApplicableDefinitionsAsync(string serviceCategoryCode, CancellationToken ct)
    {
        var cat = (serviceCategoryCode ?? string.Empty).Trim().ToUpperInvariant();
        return await _db.PricingAttributeDefinitions
            .AsNoTracking()
            .Where(d => d.IsActive && !d.IsDeleted && d.Categories.Any(c => c.ServiceCategoryCode == cat))
            .OrderBy(d => d.SortOrder).ThenBy(d => d.Code)
            .ToListAsync(ct);
    }

    /// <summary>Validate a line's attribute value set (full set for that line). Throws on the first violation.</summary>
    public async Task ValidateAsync(string serviceCategoryCode, IReadOnlyList<OfferLineAttributeValueRequest> values, CancellationToken ct)
    {
        var defs = await GetApplicableDefinitionsAsync(serviceCategoryCode, ct);

        // Resolve each referenced Lookup group's active item codes once (fail-loud on remote failure).
        var lookupMembers = new Dictionary<string, IReadOnlySet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var def in defs.Where(d => d.DataType == PricingAttributeDataType.Lookup && !string.IsNullOrWhiteSpace(d.LookupGroupCode)))
        {
            var group = def.LookupGroupCode!;
            if (lookupMembers.ContainsKey(group)) continue;
            var items = await _lookup.GetActiveItemsAsync(group, ct);
            lookupMembers[group] = items.Select(i => i.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        PricingAttributeValueValidation.Validate(defs, lookupMembers, values);
    }
}

/// <summary>
/// S2b — the pure validation core (no I/O). Given the applicable definitions and each Lookup group's resolved member
/// set, it fails loud (SR error codes) on: unknown/duplicate definition, wrong value type for the DataType, a Lookup value
/// that is not a member of its group, a required attribute missing, or a Number value outside the definition's min/max.
/// </summary>
public static class PricingAttributeValueValidation
{
    public static void Validate(
        IReadOnlyList<PricingAttributeDefinitionEntity> definitions,
        IReadOnlyDictionary<string, IReadOnlySet<string>> lookupMembersByGroup,
        IReadOnlyList<OfferLineAttributeValueRequest> values)
    {
        values ??= Array.Empty<OfferLineAttributeValueRequest>();
        var defByCode = definitions.ToDictionary(d => d.Code, StringComparer.OrdinalIgnoreCase);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var v in values)
        {
            var code = (v.DefinitionCode ?? string.Empty).Trim().ToUpperInvariant();
            if (code.Length == 0)
                throw new AizenBusinessException("SR_PRICING_ATTR_UNKNOWN_DEFINITION: (empty code)");
            if (!seen.Add(code))
                throw new AizenBusinessException($"SR_PRICING_ATTR_DUPLICATE_VALUE: '{code}'");
            if (!defByCode.TryGetValue(code, out var def))
                throw new AizenBusinessException($"SR_PRICING_ATTR_UNKNOWN_DEFINITION: '{code}'");

            ValidateValue(def, v, lookupMembersByGroup);
        }

        foreach (var def in definitions.Where(d => d.IsRequired))
            if (!seen.Contains(def.Code))
                throw new AizenBusinessException($"SR_PRICING_ATTR_REQUIRED_MISSING: '{def.Code}'");
    }

    private static void ValidateValue(
        PricingAttributeDefinitionEntity def, OfferLineAttributeValueRequest v,
        IReadOnlyDictionary<string, IReadOnlySet<string>> lookupMembersByGroup)
    {
        var hasLookup = !string.IsNullOrWhiteSpace(v.ValueLookupItemCode);
        var hasNumber = v.ValueNumber.HasValue;
        var hasText   = !string.IsNullOrWhiteSpace(v.ValueText);
        var hasBool   = v.ValueBool.HasValue;

        switch (def.DataType)
        {
            case PricingAttributeDataType.Lookup:
                if (!hasLookup || hasNumber || hasText || hasBool)
                    throw new AizenBusinessException($"SR_PRICING_ATTR_WRONG_TYPE: '{def.Code}' expects a Lookup item code");
                var members = lookupMembersByGroup.TryGetValue(def.LookupGroupCode ?? string.Empty, out var set)
                    ? set : (IReadOnlySet<string>)new HashSet<string>();
                var itemCode = v.ValueLookupItemCode!.Trim().ToUpperInvariant();
                if (!members.Contains(itemCode))
                    throw new AizenBusinessException(
                        $"SR_PRICING_ATTR_INVALID_LOOKUP_ITEM: '{itemCode}' not in group '{def.LookupGroupCode}' for '{def.Code}'");
                break;

            case PricingAttributeDataType.Number:
                if (!hasNumber || hasLookup || hasText || hasBool)
                    throw new AizenBusinessException($"SR_PRICING_ATTR_WRONG_TYPE: '{def.Code}' expects a Number");
                var n = v.ValueNumber!.Value;
                if ((def.MinValue.HasValue && n < def.MinValue.Value) || (def.MaxValue.HasValue && n > def.MaxValue.Value))
                    throw new AizenBusinessException(
                        $"SR_PRICING_ATTR_NUMBER_OUT_OF_RANGE: '{def.Code}' = {n} not in [{def.MinValue}, {def.MaxValue}]");
                break;

            case PricingAttributeDataType.Text:
                if (!hasText || hasLookup || hasNumber || hasBool)
                    throw new AizenBusinessException($"SR_PRICING_ATTR_WRONG_TYPE: '{def.Code}' expects Text");
                break;

            case PricingAttributeDataType.Boolean:
                if (!hasBool || hasLookup || hasNumber || hasText)
                    throw new AizenBusinessException($"SR_PRICING_ATTR_WRONG_TYPE: '{def.Code}' expects a Boolean");
                break;

            default:
                throw new AizenBusinessException($"SR_PRICING_ATTR_WRONG_TYPE: '{def.Code}' has an unknown DataType");
        }
    }
}
