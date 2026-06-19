using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;

[DocumentationInfo("Admin vessel media BFF response", "List of vessel media items for the Media tab.")]
public sealed class AdminVesselMediaBffResponse
{
    public List<VesselMediaBffDto> Media { get; set; } = new();
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
