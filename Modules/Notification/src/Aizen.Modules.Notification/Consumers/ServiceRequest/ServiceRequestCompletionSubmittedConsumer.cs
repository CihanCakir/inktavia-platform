using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.ServiceRequest;

public sealed class ServiceRequestCompletionSubmittedConsumer
    : AizenBaseMessageConsumer<ServiceRequestCompletionSubmittedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ServiceRequestCompletionSubmittedConsumer> _logger;

    public ServiceRequestCompletionSubmittedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ServiceRequestCompletionSubmittedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestCompletionSubmittedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestCompletionSubmittedMessage message, CancellationToken ct)
    {
        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.OwnerUserId,
            Type            = NotificationType.CompletionSubmitted,
            Channel         = NotificationChannel.InApp,
            Variables = new Dictionary<string, string>
            {
                { "serviceRequestId", message.ServiceRequestId.ToString() },
                { "completionId",     message.CompletionId.ToString() },
            },
            MetadataJson = $"{{\"serviceRequestId\":{message.ServiceRequestId},\"completionId\":{message.CompletionId}}}",
        }, ct);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestCompletionSubmittedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestCompletionSubmittedConsumer SR={SrId}: {Error}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
