using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;


public sealed class CreatePricingAttributeBffCommandHandler
    : AizenCommandHandler<CreatePricingAttributeBffCommand, PricingAttributeDefinitionDto>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;
    public CreatePricingAttributeBffCommandHandler(IServiceRequestRemoteCall serviceRequest)
        => _serviceRequest = serviceRequest;

    public override async Task<PricingAttributeDefinitionDto?> Handle(
        CreatePricingAttributeBffCommand request, CancellationToken ct)
    {
        var result = await _serviceRequest.CreateAdminPricingAttribute(request.Request);
        return result.Body;
    }
}
