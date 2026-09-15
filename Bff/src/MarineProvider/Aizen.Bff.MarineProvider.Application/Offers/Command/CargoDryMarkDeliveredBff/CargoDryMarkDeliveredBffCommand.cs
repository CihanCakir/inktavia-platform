using Aizen.Core.CQRS.Message;

namespace Aizen.Bff.MarineProvider.Application.Offers;

/// <summary>
/// CargoDry supply v2 — the assigned provider marks a supply order delivered (capturing the delivered kit). The SR
/// module gates to the assigned provider server-side and starts the delivered auto-complete window. Provider identity
/// is asserted from the token.
/// </summary>
public sealed class CargoDryMarkDeliveredBffCommand : AizenCommand<CargoDryMarkDeliveredBffResponse>
{
    public long ServiceRequestId { get; init; }
    public long KitId            { get; init; }
}

public sealed class CargoDryMarkDeliveredBffResponse
{
    public bool      Success                 { get; init; }
    public string?   Message                 { get; init; }
    public long?     ServiceRequestId        { get; init; }
    public DateTime? AutoCompleteDeadlineUtc { get; init; }
}
