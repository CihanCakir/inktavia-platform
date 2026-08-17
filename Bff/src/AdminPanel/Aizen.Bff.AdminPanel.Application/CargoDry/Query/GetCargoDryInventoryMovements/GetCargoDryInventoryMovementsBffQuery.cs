using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Query.GetCargoDryInventoryMovements;

public sealed class GetCargoDryInventoryMovementsBffQuery : AizenQuery<GetCargoDryInventoryMovementsBffResponse>
{
    public long?     ProviderProfileId { get; init; }
    public string?   ProductCode       { get; init; }
    public string?   BatchCode         { get; init; }
    public int?      MovementType      { get; init; }
    public DateTime? DateFrom          { get; init; }
    public DateTime? DateTo            { get; init; }
    public int       Page              { get; init; } = 1;
    public int       PageSize          { get; init; } = 50;
}

public sealed class GetCargoDryInventoryMovementsBffResponse
{
    public CargoDryInventoryMovementPagedBffDto? PagedResult { get; init; }
}
