using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryBatchList;

public sealed class GetCargoDryBatchListQuery : AizenQuery<CargoDryBatchListDto>
{
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    // ADDENDUM A3 — filter batches by which provider they were allocated to (and allocated-vs-unallocated).
    public long?   AssignedProviderProfileId { get; init; }
    /// <summary>null/"all" = any; "allocated" = has a provider; "unallocated" = platform stock (no provider).</summary>
    public string? AllocationState           { get; init; }
}
