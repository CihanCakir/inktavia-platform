using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Command.ResolveProfileRiskSignal;

public sealed class ResolveProfileRiskSignalBffCommand
    : AizenCommand<ResolveProfileRiskSignalBffCommandResponse>
{
    public long                                SignalId { get; init; }
    public required ResolveProfileRiskSignalBffRequest Body { get; init; }
}

public sealed class ResolveProfileRiskSignalBffCommandResponse
{
    public ProfileRiskSignalBffDto Result { get; init; } = default!;
}
