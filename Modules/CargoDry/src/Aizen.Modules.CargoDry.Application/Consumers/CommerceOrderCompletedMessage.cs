using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Modules.CargoDry.Application.Consumers;

/// <summary>Local stub until Commerce.Abstraction is available.</summary>
public sealed class CommerceOrderCompletedMessage : AizenBaseMessage
{
    public long OrderId { get; set; }
    public List<CommerceOrderItemMessage> Items { get; set; } = [];
}

public sealed class CommerceOrderItemMessage
{
    public string  ItemType    { get; set; } = default!;
    public long?   ReferenceId { get; set; }
    public int     Quantity    { get; set; }
}
