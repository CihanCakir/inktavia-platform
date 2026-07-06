using Aizen.Modules.Profile.Abstraction.Enums.Performance;

namespace Aizen.Modules.Profile.Abstraction.Dtos.Performance;

public sealed record ProfileDecisionLogDto
{
    public long                  Id               { get; init; }
    public long                  ProfileId        { get; init; }
    public ProfileType           ProfileType      { get; init; }
    public DecisionLogEventType  EventType        { get; init; }
    public string                EventTypeName    => EventType.ToString();
    public string                EventDescription { get; init; } = default!;
    public PriorityTier?         PreviousTier     { get; init; }
    public PriorityTier?         NewTier          { get; init; }
    public decimal?              PreviousScore    { get; init; }
    public decimal?              NewScore         { get; init; }
    public string?               ActorUserId      { get; init; }
    public DateTime              OccurredAtUtc    { get; init; }
    public string?               MetadataJson     { get; init; }
}
