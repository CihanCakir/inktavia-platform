using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Abstraction.Message;
using Aizen.Modules.CargoDry.Application.Commands.RenewKit;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Consumers;

public sealed class CommerceOrderCompletedConsumer
    : AizenBaseMessageConsumer<CommerceOrderCompletedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<CommerceOrderCompletedConsumer> _logger;

    public CommerceOrderCompletedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<CommerceOrderCompletedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(
        CommerceOrderCompletedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(
        CommerceOrderCompletedMessage message, CancellationToken ct)
    {
        var renewalItems = message.Items
            .Where(i => i.ItemType == "CargoDryRenewal" && i.ReferenceId.HasValue)
            .ToList();

        foreach (var item in renewalItems)
        {
            _logger.LogInformation(
                "Processing CargoDry renewal for KitId={KitId} from Order={OrderId}",
                item.ReferenceId, message.OrderId);

            await _sender.Send(new RenewKitCommand
            {
                KitId      = item.ReferenceId!.Value,
                AddedDays  = item.Quantity * 90,
                Type       = RenewalType.OnlinePurchase,
                PaymentRef = message.OrderId.ToString(),
            }, ct);
        }
    }

    public override Task ExecuteRollbackMessage(
        CommerceOrderCompletedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogError("CommerceOrderCompletedConsumer rollback for OrderId={OrderId}: {Error}",
            message.OrderId, ex.Message);
        return Task.CompletedTask;
    }
}
