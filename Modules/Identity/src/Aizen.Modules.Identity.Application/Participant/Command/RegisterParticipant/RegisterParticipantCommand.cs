
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.RegisterParticipant
{
    public sealed class RegisterParticipantCommand : AizenCommand<RegisterResult>
    {
        public string Email { get; }
        public string Password { get; }
        public string? Phone { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public bool KvkkAccepted { get; }
        public string? DeviceId { get; }
        public ConsumerDeviceType? DeviceType { get; }
        public string? NotificationToken { get; }

        public RegisterParticipantCommand(
            string email,
            string password,
            string? phone,
            string firstName,
            string lastName,
            bool kvkkAccepted,
            string? deviceId,
            ConsumerDeviceType? deviceType,
            string? notificationToken)
        {
            Email = email;
            Password = password;
            Phone = phone;
            FirstName = firstName;
            LastName = lastName;
            KvkkAccepted = kvkkAccepted;
            DeviceId = deviceId;
            DeviceType = deviceType;
            NotificationToken = notificationToken;
        }
    }
}