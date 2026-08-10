using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Notification;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Request;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Notification;

/// <summary>POST /api/v1/mobile/notifications/device-token — register the caller's FCM push token (feeds MO9a). The
/// Platform is FORCED to Fcm server-side (the Expo/native client only sends the token); the recipient comes from the
/// token via the assertion, never the body.</summary>
public sealed class RegisterMobileDeviceTokenCommand : AizenCommand<MobileDeviceTokenResultDto>
{
    public RegisterMobileDeviceTokenCommand(string? deviceToken) => DeviceToken = deviceToken;
    public string? DeviceToken { get; }
}

public sealed class RegisterMobileDeviceTokenCommandHandler
    : AizenCommandHandler<RegisterMobileDeviceTokenCommand, MobileDeviceTokenResultDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly INotificationRemoteCall _notification;

    public RegisterMobileDeviceTokenCommandHandler(IParticipantProfileResolver resolver, INotificationRemoteCall notification)
    {
        _resolver = resolver;
        _notification = notification;
    }

    public override async Task<MobileDeviceTokenResultDto?> Handle(
        RegisterMobileDeviceTokenCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        var token = request.DeviceToken?.Trim();
        if (string.IsNullOrWhiteSpace(token))
            throw new AizenBusinessException("A device token is required.");

        // Force Fcm — the mobile client is an Expo/native FCM app; never trust a body-supplied platform.
        var resp = await _notification.RegisterDeviceToken(new RegisterDeviceTokenRequest
        {
            DeviceToken = token!,
            Platform    = PushPlatform.Fcm,
        });

        var body = resp?.Body
            ?? throw new AizenBusinessException("Could not register the device token.");

        return MobileNotificationMapper.MapDeviceToken(body);
    }
}
