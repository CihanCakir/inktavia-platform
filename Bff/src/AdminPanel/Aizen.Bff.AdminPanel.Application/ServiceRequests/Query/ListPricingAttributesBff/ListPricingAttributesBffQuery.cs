using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Dto.Pricing;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Query;

/// <summary>S2a — list pricing attribute definitions (optionally scoped to a service category).</summary>
public sealed class ListPricingAttributesBffQuery : AizenQuery<List<PricingAttributeDefinitionDto>>
{
    public string? ServiceCategoryCode { get; init; }
}
