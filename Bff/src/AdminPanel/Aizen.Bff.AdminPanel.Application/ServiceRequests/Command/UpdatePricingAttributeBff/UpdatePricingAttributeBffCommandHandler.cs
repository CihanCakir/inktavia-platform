using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;


public sealed class UpdatePricingAttributeBffCommandHandler
    : AizenCommandHandler<UpdatePricingAttributeBffCommand, PricingAttributeDefinitionDto>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;
    public UpdatePricingAttributeBffCommandHandler(IServiceRequestRemoteCall serviceRequest)
        => _serviceRequest = serviceRequest;

    public override async Task<PricingAttributeDefinitionDto?> Handle(
        UpdatePricingAttributeBffCommand request, CancellationToken ct)
    {
        var result = await _serviceRequest.UpdateAdminPricingAttribute(request.Id, request.Request);
        return result.Body;
    }
}
