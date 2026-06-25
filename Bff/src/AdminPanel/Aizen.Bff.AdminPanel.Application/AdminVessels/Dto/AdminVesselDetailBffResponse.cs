using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;

[DocumentationInfo("Admin vessel detail BFF response", "Aggregated vessel detail with cross-module data for the Vessel Detail Overview page.")]
public sealed class AdminVesselDetailBffResponse
{
    public VesselDetailBffDto? Vessel { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
