using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.RegisterVenue
{
    public sealed class RegisterVenueCommand : AizenCommand<RegisterResult>
    {
        public string Email { get; }
        public string Password { get; }
        public string VenueName { get; }
        public string Address { get; }
        public string ContactPhone { get; }
        public string OwnerFirstName { get; }
        public string OwnerLastName { get; }
        public bool KvkkAccepted { get; }
        public string? DeviceId { get; }
        public ConsumerDeviceType? DeviceType { get; }
        public string? NotificationToken { get; }


        public RegisterVenueCommand(
            string email,
            string password,
            string venueName,
            string address,
            string contactPhone,
            string ownerFirstName,
            string ownerLastName,
            bool kvkkAccepted,
            string? deviceId,
            ConsumerDeviceType? deviceType,
            string? notificationToken)
        {
            Email = email;
            Password = password;
            VenueName = venueName;
            Address = address;
            ContactPhone = contactPhone;
            OwnerFirstName = ownerFirstName;
            OwnerLastName = ownerLastName;
            KvkkAccepted = kvkkAccepted;
            DeviceId = deviceId;
            DeviceType = deviceType;
            NotificationToken = notificationToken;
        }
    }

}