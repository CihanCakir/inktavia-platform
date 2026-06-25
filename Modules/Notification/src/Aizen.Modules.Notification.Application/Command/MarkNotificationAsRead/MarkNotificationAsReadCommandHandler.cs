using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.MarkNotificationAsRead;

public sealed class MarkNotificationAsReadCommandHandler
    : AizenCommandHandler<MarkNotificationAsReadCommand, bool>
{
    private readonly INotificationRepository _repository;

    public MarkNotificationAsReadCommandHandler(INotificationRepository repository)
        => _repository = repository;

    public override async Task<bool> Handle(
        MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var entity = await _repository.GetByIdAsync(request.NotificationId, cancellationToken);
        if (entity is null || entity.RecipientUserId != request.RequestingUserId)
            return false;

        entity.MarkAsRead();
        await _repository.UpdateAsync(entity, cancellationToken);
        return true;
    }
}
