using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;

namespace Aizen.Modules.ServiceRequest.Application.Services.Pricing;

/// <summary>
/// S2a — validates an admin pricing-attribute definition payload: names/scope present; when DataType = Lookup the
/// <c>LookupGroupCode</c> must resolve to an existing R4 group (fail-loud via the ReferenceData remote call); Number
/// min ≤ max. Uniqueness of <c>Code</c> is enforced in the command handler (DB). Shape validation only — no money math.
/// </summary>
public sealed class PricingAttributeDefinitionValidator
{
    private readonly ReferenceDataLookupClient _lookup;

    public PricingAttributeDefinitionValidator(ReferenceDataLookupClient lookup) => _lookup = lookup;

    public async Task ValidateAsync(PricingAttributeDefinitionRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Code))
            throw new AizenBusinessException("SR_PRICING_ATTR_CODE_REQUIRED");
        if (string.IsNullOrWhiteSpace(req.NameTr) || string.IsNullOrWhiteSpace(req.NameEn))
            throw new AizenBusinessException("SR_PRICING_ATTR_NAME_REQUIRED");

        var categories = (req.ServiceCategoryCodes ?? new())
            .Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        if (categories.Count == 0)
            throw new AizenBusinessException("SR_PRICING_ATTR_SCOPE_REQUIRED");

        if (req.DataType == PricingAttributeDataType.Lookup)
        {
            if (string.IsNullOrWhiteSpace(req.LookupGroupCode))
                throw new AizenBusinessException("SR_PRICING_ATTR_GROUP_REQUIRED");
            // Fail-loud: an unknown group resolves to an empty item set.
            if (!await _lookup.GroupResolvesAsync(req.LookupGroupCode, ct))
                throw new AizenBusinessException($"SR_PRICING_ATTR_UNKNOWN_GROUP: '{req.LookupGroupCode.Trim().ToUpperInvariant()}'");
        }

        if (req.DataType == PricingAttributeDataType.Number
            && req.MinValue.HasValue && req.MaxValue.HasValue && req.MinValue.Value > req.MaxValue.Value)
            throw new AizenBusinessException($"SR_PRICING_ATTR_MIN_MAX: min {req.MinValue} > max {req.MaxValue}");
    }
}
