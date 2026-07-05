using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryRenewalPreparationsPaged;

[DocumentationInfo("Get CargoDry renewal preparations paged query",
    "Returns paged list of renewal preparations with optional filters. " +
    "Phase 11 (July 2026).")]
public sealed class GetCargoDryRenewalPreparationsPagedQuery
    : AizenQuery<GetCargoDryRenewalPreparationsPagedResponse>
{
    public long?                              KitId              { get; init; }
    public string?                            KitCode            { get; init; }
    public string?                            ProductCode        { get; init; }
    public long?                              OwnerUserId        { get; init; }
    public long?                              VesselId           { get; init; }
    public CargoDryRenewalPreparationStatus?  Status             { get; init; }
    public CargoDryRenewalNotificationStatus? NotificationStatus { get; init; }
    public DateTimeOffset?                    PreparedFrom       { get; init; }
    public DateTimeOffset?                    PreparedTo         { get; init; }
    public int                                Page               { get; init; } = 1;
    public int                                PageSize           { get; init; } = 25;
}

public sealed class GetCargoDryRenewalPreparationsPagedResponse
{
    public List<CargoDryRenewalPreparationDto> Items    { get; init; } = new();
    public int                                 Total    { get; init; }
    public int                                 Page     { get; init; }
    public int                                 PageSize { get; init; }
    public int                                 Pages    { get; init; }
}
