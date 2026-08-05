using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.PricingAttributes;

/// <summary>S2a — update a pricing attribute definition by id (the code is immutable server-side).</summary>
public sealed class UpdatePricingAttributeCommand : AizenCommand<PricingAttributeDefinitionDto>
{
    public long Id { get; init; }
    public PricingAttributeDefinitionRequest Request { get; init; } = default!;
}

public sealed class UpdatePricingAttributeCommandHandler
    : AizenCommandHandler<UpdatePricingAttributeCommand, PricingAttributeDefinitionDto>
{
    private readonly IServiceRequestAdminBffRemoteCall _serviceRequest;
    public UpdatePricingAttributeCommandHandler(IServiceRequestAdminBffRemoteCall serviceRequest)
        => _serviceRequest = serviceRequest;

    public override async Task<PricingAttributeDefinitionDto?> Handle(
        UpdatePricingAttributeCommand request, CancellationToken ct)
    {
        var result = await _serviceRequest.UpdateAdminPricingAttribute(request.Id, request.Request);
        return result.Body;
    }
}
