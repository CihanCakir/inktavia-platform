namespace Aizen.Bff.AdminPanel.Application.Dashboard.Dto;

[DocumentationInfo("Status count DTO",
    "One (status → count) row for a dashboard chart — SR volume (C2) or vessel fleet status (C3). Status is the lowercase key the FE maps to a label.")]
public sealed class StatusCountDto
{
    public string Status { get; set; } = default!;
    public int Count { get; set; }
}
