using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.CargoDry.Abstraction.Message;

public sealed class CargoDryProviderMilestoneReachedMessage : AizenBaseMessage
{
    public long   ProviderProfileId { get; set; }
    public string MilestoneType     { get; set; } = default!;
    public string PeriodKey         { get; set; } = default!;
    public string? DisplayValue     { get; set; }
    public DateTime OccurredAtUtc   { get; set; }
}
