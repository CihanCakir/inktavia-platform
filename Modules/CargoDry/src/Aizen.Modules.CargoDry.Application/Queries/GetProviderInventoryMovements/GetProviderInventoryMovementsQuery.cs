using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;

namespace Aizen.Modules.CargoDry.Application.Queries.GetProviderInventoryMovements;

public sealed class GetProviderInventoryMovementsQuery : AizenQuery<CargoDryInventoryMovementPagedResultDto>
{
    public long?                  ProviderProfileId { get; init; }
    public string?                ProductCode       { get; init; }
    public string?                BatchCode         { get; init; }
    public InventoryMovementType? MovementType      { get; init; }
    public DateTime?              DateFrom          { get; init; }
    public DateTime?              DateTo            { get; init; }
    public int                    Page              { get; init; } = 1;
    public int                    PageSize          { get; init; } = 50;
}
