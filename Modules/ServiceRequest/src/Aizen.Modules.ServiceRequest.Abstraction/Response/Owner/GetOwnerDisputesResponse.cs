namespace Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;

/// <summary>
/// BE-MO5 — an owner's own disputes: the disputes on the service requests they own (<c>sr.OwnerUserId</c>). Mirrors
/// the provider list (<c>GetProviderDisputesResponse</c>). Cost-free (§20.9): carries only the dispute + its SR header,
/// never supplier cost / dealer margin / offer economics. <see cref="OpenCount"/> is the owner's global count of
/// open/actionable disputes (independent of the page filter).
/// </summary>
[DocumentationInfo("Get owner disputes response", "Paged list of the calling owner's disputes + a global open/actionable count.")]
public sealed class GetOwnerDisputesResponse
{
    public List<OwnerDisputeItemDto> Items { get; init; } = new();
    public int PageIndex { get; init; }
    public int PageSize { get; init; }
    /// <summary>Total disputes matching the (optionally status-filtered) query — for paging.</summary>
    public int Total { get; init; }
    /// <summary>The owner's open/actionable disputes (status not Resolved/Closed), global — independent of the page filter.</summary>
    public int OpenCount { get; init; }
    public int TotalReturned => Items.Count;
}

/// <summary>One dispute row for an owner. Cost-free — no economics fields.</summary>
public sealed record OwnerDisputeItemDto
{
    public long DisputeId { get; init; }
    public long ServiceRequestId { get; init; }
    public string ServiceRequestCode { get; init; } = string.Empty;
    public string ServiceRequestTitle { get; init; } = string.Empty;
    public string? ServiceCategoryCode { get; init; }
    /// <summary><c>ServiceRequestDisputeStatus</c> name (e.g. "Open", "PendingOwnerResponse", "Resolved").</summary>
    public string Status { get; init; } = string.Empty;
    /// <summary><c>ServiceRequestDisputeReason</c> name (e.g. "QualityIssue").</summary>
    public string Reason { get; init; } = string.Empty;
    public string? Description { get; init; }
    /// <summary>True while the dispute is still open/actionable (status not Resolved/Closed).</summary>
    public bool IsOpen { get; init; }
    /// <summary>True when this owner opened the dispute (vs the provider/admin).</summary>
    public bool OpenedByMe { get; init; }
    public DateTime OpenedAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}
