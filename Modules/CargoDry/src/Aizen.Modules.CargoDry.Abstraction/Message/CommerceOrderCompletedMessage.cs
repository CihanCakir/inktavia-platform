using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.CargoDry.Abstraction.Message;

/// <summary>
/// Published when a commerce order (e-commerce purchase) is completed.
/// CargoDry consumer uses this to trigger kit renewals for CargoDryRenewal line items.
/// </summary>
public sealed class CommerceOrderCompletedMessage : AizenBaseMessage
{
    public long                            OrderId { get; init; }
    public List<CommerceOrderItemMessage>  Items   { get; init; } = [];
}

public sealed class CommerceOrderItemMessage
{
    public string  ItemType    { get; init; } = default!;
    public long?   ReferenceId { get; init; }
    public int     Quantity    { get; init; }
}
