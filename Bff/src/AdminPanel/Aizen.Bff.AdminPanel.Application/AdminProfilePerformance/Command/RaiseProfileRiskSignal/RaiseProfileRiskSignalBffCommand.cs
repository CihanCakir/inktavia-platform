using Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Dto;
using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.AdminPanel.Application.AdminProfilePerformance.Command.RaiseProfileRiskSignal;

public sealed class RaiseProfileRiskSignalBffCommand
    : AizenCommand<RaiseProfileRiskSignalBffCommandResponse>
{
    public long                              ProfileId   { get; init; }
    public string                            ProfileType { get; init; } = "Provider";
    public required RaiseProfileRiskSignalBffRequest Body { get; init; }
}

public sealed class RaiseProfileRiskSignalBffCommandResponse
{
    public ProfileRiskSignalBffDto Result { get; init; } = default!;
}
