using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Response;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Participant
{
    public class CompleteExternalLoginParticipantCommand : AizenCommand<UserLoginResponse>
    {
        public string Provider { get; }
        public string Code { get; }
        public string State { get; }
        public string? CodeVerifier { get; }
        public string? DeviceId { get; }
        public ConsumerDeviceType DeviceType { get; }
        public string? NotificationToken { get; }
        public string? UserAgent { get; }
        public string? Ip { get; }

        public CompleteExternalLoginParticipantCommand(
            string provider,              // "google" | "apple"
            string code,
            string state,
            string? codeVerifier,
            string? deviceId,
            ConsumerDeviceType deviceType,
            string? notificationToken,
            string? userAgent,
            string? ip
        )
        {
            Provider = provider;
            Code = code;
            State = state;
            CodeVerifier = codeVerifier;
            DeviceId = deviceId;
            DeviceType = deviceType;
            NotificationToken = notificationToken;
            UserAgent = userAgent;
            Ip = ip;
        }
    }
}