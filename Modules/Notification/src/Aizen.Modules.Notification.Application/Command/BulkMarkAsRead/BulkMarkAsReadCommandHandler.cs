using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.BulkMarkAsRead;

public sealed class BulkMarkAsReadCommandHandler
    : AizenCommandHandler<BulkMarkAsReadCommand, bool>
{
    private readonly INotificationRepository _repository;

    public BulkMarkAsReadCommandHandler(INotificationRepository repository)
        => _repository = repository;

    public override async Task<bool> Handle(
        BulkMarkAsReadCommand request, CancellationToken cancellationToken)
    {
        await _repository.BulkMarkAsReadAsync(request.UserId, cancellationToken);
        return true;
    }
}
