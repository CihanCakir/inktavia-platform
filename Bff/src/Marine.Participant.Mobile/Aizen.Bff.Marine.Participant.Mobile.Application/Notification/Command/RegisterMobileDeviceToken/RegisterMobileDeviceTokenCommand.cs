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
