using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;


public sealed class ListPricingAttributesBffQueryHandler
    : AizenQueryHandler<ListPricingAttributesBffQuery, List<PricingAttributeDefinitionDto>>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;
    public ListPricingAttributesBffQueryHandler(IServiceRequestRemoteCall serviceRequest)
        => _serviceRequest = serviceRequest;

    public override async Task<List<PricingAttributeDefinitionDto>?> Handle(
        ListPricingAttributesBffQuery request, CancellationToken ct)
    {
        var result = await _serviceRequest.GetAdminPricingAttributes(request.ServiceCategoryCode);
        return result.Body ?? new List<PricingAttributeDefinitionDto>();
    }
}
