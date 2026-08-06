using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Dto;

[DocumentationInfo("Admin vessel form options response", "Static option lists for vessel type and status dropdowns.")]
public sealed class AdminVesselFormOptionsResponse
{
    public List<string>? VesselTypes { get; set; }
    public List<string>? StatusOptions { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
