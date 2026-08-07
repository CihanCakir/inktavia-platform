namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Provider;

/// <summary>
/// A provider's own disputes — the disputes opened on the service requests they won (accepted offer). Cost-free
/// (§20.9): carries only the dispute + its SR header, never supplier cost / dealer margin / offer economics.
/// <see cref="OpenCount"/> is the provider's global count of open/actionable disputes (independent of the page
/// filter) — the number the dashboard "disputes needing input" attention row reads.
/// </summary>
[DocumentationInfo("Get provider disputes response", "Paged list of the calling provider's disputes + a global open/actionable count.")]
public sealed class GetProviderDisputesResponse
{
    public List<ProviderDisputeItemDto> Items { get; init; } = new();
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
    /// <summary>Total disputes matching the (optionally status-filtered) query — for paging.</summary>
    public int Total { get; init; }
    /// <summary>The provider's open/actionable disputes (status not Resolved/Closed), global — for the dashboard row.</summary>
    public int OpenCount { get; init; }
    public int TotalReturned => Items.Count;
}

/// <summary>One dispute row for a provider. Cost-free — no economics fields.</summary>
public sealed record ProviderDisputeItemDto
{
    public long DisputeId { get; init; }
    public long ServiceRequestId { get; init; }
    public string ServiceRequestCode { get; init; } = string.Empty;
    public string ServiceRequestTitle { get; init; } = string.Empty;
    public string? ServiceCategoryCode { get; init; }
    /// <summary><c>ServiceRequestDisputeStatus</c> name (e.g. "Open", "PendingProviderResponse", "Resolved").</summary>
    public string Status { get; init; } = string.Empty;
    /// <summary><c>ServiceRequestDisputeReason</c> name (e.g. "QualityIssue").</summary>
    public string Reason { get; init; } = string.Empty;
    public string? Description { get; init; }
    /// <summary>True while the dispute is still open/actionable (status not Resolved/Closed).</summary>
    public bool IsOpen { get; init; }
    /// <summary>True when this provider opened the dispute (vs the owner/admin).</summary>
    public bool OpenedByMe { get; init; }
    public DateTime OpenedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}
