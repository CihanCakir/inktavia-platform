using Aizen.Bff.AdminPanel.Application.CargoDry.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.CargoDry.Command.ResolveMonthlySellThroughSettlement;

public sealed class ResolveMonthlySellThroughSettlementBffCommand
    : AizenCommand<ResolveMonthlySellThroughSettlementBffCommandResponse>
{
    public long    SettlementId     { get; init; }
    public long    ResolvedByUserId { get; init; }
    public string? ResolutionNote   { get; init; }
}

public sealed class ResolveMonthlySellThroughSettlementBffCommandResponse
{
    public CargoDrySellThroughSettlementBffDto? Settlement { get; init; }
}
