using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.Vessels.Dto;

[DocumentationInfo("Admin vessel list BFF response", "Paged vessel list with UI-ready fields and warnings.")]
public sealed class AdminVesselListBffResponse
{
    public VesselPageBffDto? Vessels { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}

[DocumentationInfo("Vessel page BFF DTO", "Pagination wrapper for vessel list BFF response.")]
public sealed class VesselPageBffDto
{
    public int From { get; set; }
    public int Index { get; set; }
    public int Size { get; set; }
    public long Count { get; set; }
    public int Pages { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
    public List<VesselListItemBffDto> Items { get; set; } = new();
}
