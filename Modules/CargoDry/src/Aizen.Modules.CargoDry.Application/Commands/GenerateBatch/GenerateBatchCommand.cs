using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;

namespace Aizen.Modules.CargoDry.Application.Commands.GenerateBatch;

public sealed class GenerateBatchCommand : AizenCommand<GenerateBatchResultDto>
{
    public string  ProductCode      { get; init; } = default!;
    public int     Count            { get; init; }
    public long    AdminUserId      { get; init; }
    /// <summary>Optional internal batch label (e.g. "Q3-REPLENISH-2024").</summary>
    public string? BatchLabel       { get; init; }
    /// <summary>Optional warehouse/dispatch location code (e.g. "SGP-MAIN").</summary>
    public string? WarehouseCode    { get; init; }
    /// <summary>Optional free-text production notes.</summary>
    public string? ProductionNotes  { get; init; }
}
