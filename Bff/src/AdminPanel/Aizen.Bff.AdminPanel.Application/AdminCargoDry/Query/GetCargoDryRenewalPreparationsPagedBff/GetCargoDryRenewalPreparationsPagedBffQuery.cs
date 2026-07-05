using Aizen.Bff.AdminPanel.Application.AdminCargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryRenewalPreparationsPagedBff;

[DocumentationInfo("Get CargoDry renewal preparations paged BFF query",
    "Returns paginated list of renewal preparations with optional filters. " +
    "Phase 11 (July 2026).")]
public sealed class GetCargoDryRenewalPreparationsPagedBffQuery
    : AizenQuery<GetCargoDryRenewalPreparationsPagedBffResponse>
{
    public long?           KitId              { get; init; }
    public string?         KitCode            { get; init; }
    public string?         ProductCode        { get; init; }
    public long?           OwnerUserId        { get; init; }
    public long?           VesselId           { get; init; }
    public int?            Status             { get; init; }
    public int?            NotificationStatus { get; init; }
    public DateTimeOffset? PreparedFrom       { get; init; }
    public DateTimeOffset? PreparedTo         { get; init; }
    public int             Page               { get; init; } = 1;
    public int             PageSize           { get; init; } = 25;
}

public sealed class GetCargoDryRenewalPreparationsPagedBffResponse
{
    public CargoDryRenewalPreparationsPagedBffResponse? PagedResult { get; init; }
}
