namespace Aizen.Bff.AdminPanel.Application.Common.Dto;

[DocumentationInfo("Admin BFF bulk command result", "Result DTO for bulk operations with per-item success/failure tracking.")]
public sealed class AdminBffBulkCommandResultDto
{
    public int TotalRequested { get; }
    public int Succeeded { get; }
    public int Failed { get; }
    public IReadOnlyList<string> Errors { get; }

    public AdminBffBulkCommandResultDto(int totalRequested, int succeeded, IReadOnlyList<string>? errors = null)
    {
        TotalRequested = totalRequested;
        Succeeded = succeeded;
        Failed = totalRequested - succeeded;
        Errors = errors ?? Array.Empty<string>();
    }
}
