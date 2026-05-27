using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.RegisterOrganizer
{
public sealed class RegisterOrganizerCommand : AizenCommand<RegisterResult>
{
    public string Email { get; }
    public string Password { get; }
    public string CompanyName { get; }
    public string TaxNo { get; }
    public string ContactPhone { get; }
    public string OwnerFirstName { get; }
    public string OwnerLastName { get; }
    public bool KvkkAccepted { get; }
    public string? DeviceId { get; }
    public ConsumerDeviceType? DeviceType { get; }
    public string? NotificationToken { get; }

    public RegisterOrganizerCommand(
        string email,
        string password,
        string companyName,
        string taxNo,
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
        CompanyName = companyName;
        TaxNo = taxNo;
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