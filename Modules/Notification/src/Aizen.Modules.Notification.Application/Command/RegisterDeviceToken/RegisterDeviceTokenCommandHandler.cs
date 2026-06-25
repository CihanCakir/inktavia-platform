using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Notification.Domain.Interface.Repository;

namespace Aizen.Modules.Notification.Application.Command.RegisterDeviceToken;

public sealed class RegisterDeviceTokenCommandHandler
    : AizenCommandHandler<RegisterDeviceTokenCommand, bool>
{
    private readonly IUserDeviceTokenRepository _repository;

    public RegisterDeviceTokenCommandHandler(IUserDeviceTokenRepository repository)
        => _repository = repository;

    public override async Task<bool> Handle(
        RegisterDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        await _repository.UpsertAsync(request.UserId, request.DeviceToken, request.Platform, cancellationToken);
        return true;
    }
}
