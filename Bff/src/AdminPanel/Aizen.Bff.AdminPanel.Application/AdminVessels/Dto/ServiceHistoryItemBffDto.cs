using Aizen.Bff.AdminPanel.Application.Common;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;

[DocumentationInfo("Service history item BFF DTO", "Null-safe service history entry for vessel detail.")]
public sealed class ServiceHistoryItemBffDto
{
    public long Id { get; set; }
    public DateTime? Date { get; set; }
    public string? ServiceType { get; set; }
    public string? Status { get; set; }
    public string? Notes { get; set; }
}
