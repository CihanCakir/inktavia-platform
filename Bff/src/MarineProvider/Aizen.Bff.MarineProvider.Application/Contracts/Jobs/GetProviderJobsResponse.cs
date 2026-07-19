using Aizen.Bff.MarineProvider.Application.Common.Warnings;

namespace Aizen.Bff.MarineProvider.Application.Contracts.Jobs;

/// <summary>
/// Provider-facing view of the caller's assigned jobs. <see cref="HasProfileLink"/> is false when the account is
/// not yet linked to a provider profile (the list is then empty). Downstream failures are surfaced as non-fatal
/// warnings rather than errors so the portal can degrade gracefully.
/// </summary>
public sealed class GetProviderJobsResponse
{
    public bool HasProfileLink { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalReturned { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<ProviderJobDto> Items { get; set; } = new();
    public List<ProviderBffWarning> Warnings { get; set; } = new();
}

public sealed class ProviderJobDto
{
    public long AssignmentId { get; set; }
    public long ServiceRequestId { get; set; }
    public long ServiceRequestOfferId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string? RequestCode { get; set; }
    public long VesselId { get; set; }
    public string? VesselName { get; set; }
    public DateTime? ScheduledStartDate { get; set; }
    public DateTime? ScheduledEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }
    public string? ProviderNotes { get; set; }
}
