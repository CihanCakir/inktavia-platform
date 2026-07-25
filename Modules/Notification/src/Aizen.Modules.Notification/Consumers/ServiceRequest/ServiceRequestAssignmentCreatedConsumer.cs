using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Application.Command.SendNotification;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Consumers.ServiceRequest;

public sealed class ServiceRequestAssignmentCreatedConsumer
    : AizenBaseMessageConsumer<ServiceRequestAssignmentCreatedMessage>
{
    private readonly ISender _sender;
    private readonly ILogger<ServiceRequestAssignmentCreatedConsumer> _logger;

    public ServiceRequestAssignmentCreatedConsumer(IServiceProvider sp) : base(sp)
    {
        _sender = sp.GetRequiredService<ISender>();
        _logger = sp.GetRequiredService<ILogger<ServiceRequestAssignmentCreatedConsumer>>();
    }

    public override Task<bool> ExecutePrepareMessage(ServiceRequestAssignmentCreatedMessage message, CancellationToken ct)
        => Task.FromResult(true);

    public override async Task ExecuteCommitMessage(ServiceRequestAssignmentCreatedMessage message, CancellationToken ct)
    {
        var variables = new Dictionary<string, string>
        {
            { "serviceRequestId", message.ServiceRequestId.ToString() },
            { "assignmentId",     message.AssignmentId.ToString() },
        };

        if (message.ScheduledStartDate.HasValue)
            variables["scheduledStartDate"] = message.ScheduledStartDate.Value.ToString("O");

        await _sender.Send(new SendNotificationCommand
        {
            RecipientUserId = message.ProviderUserId,
            Type            = NotificationType.AssignmentCreated,
            Channel         = NotificationChannel.InApp,
            Variables       = variables,
            MetadataJson    = $"{{\"serviceRequestId\":{message.ServiceRequestId},\"assignmentId\":{message.AssignmentId}}}",
            ReferenceType = "ServiceRequest",
            ReferenceId   = message.ServiceRequestId,
        }, ct);
    }

    public override Task ExecuteRollbackMessage(ServiceRequestAssignmentCreatedMessage message, AizenMessageError ex, CancellationToken ct)
    {
        _logger.LogWarning("Rollback: ServiceRequestAssignmentCreatedConsumer SR={SrId}: {Error}", message.ServiceRequestId, ex.Message);
        return Task.CompletedTask;
    }
}
