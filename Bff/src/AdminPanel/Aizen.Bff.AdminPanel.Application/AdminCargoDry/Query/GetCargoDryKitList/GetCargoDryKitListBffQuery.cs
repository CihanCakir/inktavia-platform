using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminCargoDry.Query.GetCargoDryKitList;

public sealed class GetCargoDryKitListBffQuery : AizenQuery<GetCargoDryKitListBffResponse>
{
    public string? Status      { get; init; }
    public string? Search      { get; init; }
    public long?   VesselId    { get; init; }
    public long?   OwnerUserId { get; init; }
    public string? BatchCode   { get; init; }
    public int     Page        { get; init; } = 1;
    public int     PageSize    { get; init; } = 25;
}

public sealed class GetCargoDryKitListBffResponse
{
    public CargoDryKitListBffDto KitList { get; init; } = default!;
}
