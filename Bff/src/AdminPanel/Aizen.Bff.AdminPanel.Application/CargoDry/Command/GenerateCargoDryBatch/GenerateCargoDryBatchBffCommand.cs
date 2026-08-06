using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.GenerateCargoDryBatch;

public sealed class GenerateCargoDryBatchBffCommand : AizenCommand<GenerateCargoDryBatchBffResponse>
{
    public string  ProductCode     { get; init; } = default!;
    public int     Count           { get; init; }
    public string? BatchLabel      { get; init; }
    public string? WarehouseCode   { get; init; }
    public string? ProductionNotes { get; init; }
}

public sealed class GenerateCargoDryBatchBffResponse
{
    public GenerateBatchBffResultDto Result { get; init; } = default!;
}
