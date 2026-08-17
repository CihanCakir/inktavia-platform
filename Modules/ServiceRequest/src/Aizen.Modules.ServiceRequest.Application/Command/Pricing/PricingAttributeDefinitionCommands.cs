using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;

namespace Aizen.Modules.ServiceRequest.Application.Command.Pricing;

/// <summary>S2a — admin CRUD over pricing attribute definitions.</summary>
public sealed class CreatePricingAttributeDefinitionCommand : AizenCommand<PricingAttributeDefinitionDto>
{
    public PricingAttributeDefinitionRequest Request { get; }
    public CreatePricingAttributeDefinitionCommand(PricingAttributeDefinitionRequest request) => Request = request;
}

public sealed class UpdatePricingAttributeDefinitionCommand : AizenCommand<PricingAttributeDefinitionDto>
{
    public long Id { get; }
    public PricingAttributeDefinitionRequest Request { get; }
    public UpdatePricingAttributeDefinitionCommand(long id, PricingAttributeDefinitionRequest request) { Id = id; Request = request; }
}

public sealed class DeletePricingAttributeDefinitionCommand : AizenCommand<bool>
{
    public long Id { get; }
    public DeletePricingAttributeDefinitionCommand(long id) => Id = id;
}

public sealed class ListPricingAttributeDefinitionsQuery : AizenQuery<List<PricingAttributeDefinitionDto>>
{
    /// <summary>Optional filter: only definitions scoped to this ServiceCategoryCode.</summary>
    public string? ServiceCategoryCode { get; }
    public ListPricingAttributeDefinitionsQuery(string? serviceCategoryCode = null) => ServiceCategoryCode = serviceCategoryCode;
}
