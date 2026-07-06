using Aizen.Core.CQRS.Message;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;

namespace Aizen.Modules.Profile.Application.Commands.Performance.ResolveRiskSignal;

public sealed class ResolveRiskSignalCommand : AizenCommand<ProfileRiskSignalDto>
{
    public long    SignalId       { get; init; }
    public string? ResolutionNote { get; init; }
    /// <summary>Keycloak sub of the resolving admin.</summary>
    public long?   ResolvedByUserId { get; init; }
    /// <summary>Keycloak sub string used for decision log ActorUserId field.</summary>
    public string? ActorUserId    { get; init; }
}
