using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;

namespace Aizen.Bff.AdminPanel.Application.AdminServiceRequests.Dto;

[DocumentationInfo("ServiceRequest vessel history BFF response", "Service history for a specific vessel. Used by vessel detail serviceHistory tab and dedicated history endpoint.")]
public sealed class ServiceRequestVesselHistoryBffResponse
{
    public IReadOnlyList<ServiceHistoryItemBffDto> ServiceHistory { get; set; } = Array.Empty<ServiceHistoryItemBffDto>();
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
