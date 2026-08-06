using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Pricing;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Command;

/// <summary>S2a — update a pricing attribute definition by id (the code is immutable server-side).</summary>
public sealed class UpdatePricingAttributeBffCommand : AizenCommand<PricingAttributeDefinitionDto>
{
    public long Id { get; init; }
    public PricingAttributeDefinitionRequest Request { get; init; } = default!;
}
