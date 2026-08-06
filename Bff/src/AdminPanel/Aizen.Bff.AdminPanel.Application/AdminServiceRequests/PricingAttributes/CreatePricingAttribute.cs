using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.PricingAttributes;

/// <summary>S2a — create a pricing attribute definition. Server enforces the unique code + category scoping.</summary>
public sealed class CreatePricingAttributeCommand : AizenCommand<PricingAttributeDefinitionDto>
{
    public PricingAttributeDefinitionRequest Request { get; init; } = default!;
}

public sealed class CreatePricingAttributeCommandHandler
    : AizenCommandHandler<CreatePricingAttributeCommand, PricingAttributeDefinitionDto>
{
    private readonly IServiceRequestRemoteCall _serviceRequest;
    public CreatePricingAttributeCommandHandler(IServiceRequestRemoteCall serviceRequest)
        => _serviceRequest = serviceRequest;

    public override async Task<PricingAttributeDefinitionDto?> Handle(
        CreatePricingAttributeCommand request, CancellationToken ct)
    {
        var result = await _serviceRequest.CreateAdminPricingAttribute(request.Request);
        return result.Body;
    }
}
