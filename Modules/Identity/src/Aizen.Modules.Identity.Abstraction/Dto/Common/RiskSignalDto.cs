namespace Aizen.Modules.Identity.Abstraction.Dto.Common;

public class RiskSignalDto
{
    public string Severity { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string SignalCode { get; set; } = null!;
    public string? DetectedAt { get; set; }
}
