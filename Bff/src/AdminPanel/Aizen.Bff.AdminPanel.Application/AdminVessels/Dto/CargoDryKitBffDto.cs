using Aizen.Bff.AdminPanel.Application.Common;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;

[DocumentationInfo("CargoDry kit BFF DTO", "Null-safe CargoDry kit activation data for vessel detail.")]
public sealed class CargoDryKitBffDto
{
    public string? KitCode { get; set; }
    public string? ProductName { get; set; }
    public DateTime? ActivatedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public double? EfficiencyPercent { get; set; }
    public string? Status { get; set; }
}
