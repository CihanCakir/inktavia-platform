using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.PricingAttributes;

/// <summary>S2a — list pricing attribute definitions (optionally scoped to a service category).</summary>
public sealed class ListPricingAttributesQuery : AizenQuery<List<PricingAttributeDefinitionDto>>
{
    public string? ServiceCategoryCode { get; init; }
}

public sealed class ListPricingAttributesQueryHandler
    : AizenQueryHandler<ListPricingAttributesQuery, List<PricingAttributeDefinitionDto>>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    public ListPricingAttributesQueryHandler(IServiceRequestAdminBffRemoteCall serviceRequest)
        => _serviceRequest = serviceRequest;

    public override async Task<List<PricingAttributeDefinitionDto>?> Handle(
        ListPricingAttributesQuery request, CancellationToken ct)
    {
        var result = await _serviceRequest.GetAdminPricingAttributes(request.ServiceCategoryCode);
        return result.Body ?? new List<PricingAttributeDefinitionDto>();
    }
}
