using Aizen.Core.CQRS.Message;
using Aizen.Modules.Profile.Abstraction.Dtos.Performance;
using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Application.Commands.Performance.RaiseRiskSignal;

public sealed class RaiseRiskSignalCommand : AizenCommand<ProfileRiskSignalDto>
{
    public long               ProfileId      { get; init; }
    public ProfileType        ProfileType    { get; init; }
    public RiskSignalSeverity Severity       { get; init; }
    /// <summary>Machine-readable code (e.g. "HIGH_DISPUTE_RATE", "PAYOUT_FAILURE_REPEAT").</summary>
    public string             SignalCode     { get; init; } = default!;
    public string             Description    { get; init; } = default!;
    public string?            SourceModule   { get; init; }
    public long?              SourceEntityId { get; init; }
    /// <summary>Keycloak sub of the actor raising the signal (null = system-raised).</summary>
    public string?            ActorUserId    { get; init; }
}
