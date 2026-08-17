using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

/// <summary>S2a — create a pricing attribute definition. Server enforces the unique code + category scoping.</summary>
public sealed class CreatePricingAttributeBffCommand : AizenCommand<PricingAttributeDefinitionDto>
{
    public PricingAttributeDefinitionRequest Request { get; init; } = default!;
}
