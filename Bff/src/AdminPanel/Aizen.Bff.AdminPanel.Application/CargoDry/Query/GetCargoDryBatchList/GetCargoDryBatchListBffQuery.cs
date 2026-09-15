using Aizen.Bff.AdminPanel.Application.Vessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryBatchList;

public sealed class GetCargoDryBatchListBffQuery : AizenQuery<GetCargoDryBatchListBffResponse>
{
    public int Page     { get; init; } = 1;
    public int PageSize { get; init; } = 25;
    // ADDENDUM A3 — filter by allocated provider / allocation state (batch DTO already carries AssignedProviderProfileId).
    public long?   AssignedProviderProfileId { get; init; }
    public string? AllocationState           { get; init; }
}

public sealed class GetCargoDryBatchListBffResponse
{
    public CargoDryBatchListBffDto BatchList { get; init; } = default!;
}
