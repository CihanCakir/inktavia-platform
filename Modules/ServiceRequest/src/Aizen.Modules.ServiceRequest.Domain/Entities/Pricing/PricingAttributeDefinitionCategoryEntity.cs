using Aizen.Core.Domain;

namespace Aizen.Modules.ServiceRequest.Domain.Entities.Pricing;

/// <summary>
/// S2a applicability scope — join row binding a <see cref="PricingAttributeDefinitionEntity"/> to one ServiceRequest
/// <c>ServiceCategoryCode</c>. The applicable definitions for an SR of category X are those with a join row for X.
/// </summary>
public sealed class PricingAttributeDefinitionCategoryEntity : AizenEntityWithAudit
{
    public long   PricingAttributeDefinitionId { get; private set; }
    public string ServiceCategoryCode          { get; private set; } = default!;

    private PricingAttributeDefinitionCategoryEntity() { }

    public static PricingAttributeDefinitionCategoryEntity Create(string serviceCategoryCode)
        => new()
        {
            ServiceCategoryCode = serviceCategoryCode.Trim().ToUpperInvariant(),
            IsActive            = true,
        };
}
