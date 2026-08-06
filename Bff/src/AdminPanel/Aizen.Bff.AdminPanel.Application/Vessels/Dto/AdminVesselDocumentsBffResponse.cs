using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Dto;

[DocumentationInfo("Admin vessel documents BFF response", "List of vessel documents with computed expiry and status for the Documents tab.")]
public sealed class AdminVesselDocumentsBffResponse
{
    public List<VesselDocumentBffDto> Documents { get; set; } = new();
    public List<AdminBffWarning> Warnings { get; set; } = new();
}
