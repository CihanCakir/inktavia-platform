using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;

[DocumentationInfo("Admin service request offers response", "Provider offers and agreement timeline for a service request in the admin panel.")]
public sealed class AdminServiceRequestOffersResponse
{
    public GetProviderOffersResponse? Offers { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
