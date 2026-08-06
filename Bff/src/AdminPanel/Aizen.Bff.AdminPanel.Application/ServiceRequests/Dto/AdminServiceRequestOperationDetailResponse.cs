using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;

namespace Aizen.Bff.AdminPanel.Application.ServiceRequests.Dto;

[DocumentationInfo("Admin service request operation detail response",
    "Service request detail for the admin operation panel, enriched with vessel name and provider display names.")]
public sealed class AdminServiceRequestOperationDetailResponse
{
    public GetServiceRequestDetailResponse? ServiceRequest { get; set; }

    /// <summary>Vessel name resolved from the Vessel module — avoids the frontend showing "Vessel #123".</summary>
    public string? VesselName { get; set; }

    /// <summary>
    /// Map of providerUserId → full display name resolved from the Identity bulk endpoint.
    /// Covers all provider user IDs found in Offers and the active Assignment.
    /// </summary>
    public Dictionary<long, string> ProviderNames { get; set; } = new();

    public List<AdminBffWarning> Warnings { get; set; } = new();
}
