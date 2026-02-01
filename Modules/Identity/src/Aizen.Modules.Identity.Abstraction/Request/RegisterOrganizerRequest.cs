using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Identity.Abstraction.Request
{

public sealed class RegisterOrganizerRequest
{
    public string Email { get; set; } = default!;
    public string? Phone { get; set; }
        public string CompanyName { get; set; } = default!;
        public string TaxNo { get; set; } = default!;

    public string Password { get; set; } = default!;
    public string OwnerFirstName { get; set; } = default!;
    public string OwnerLastName { get; set; } = default!;
    public bool KvkkAccepted { get; set; }

    public string? DeviceId { get; set; }
    public ConsumerDeviceType? DeviceType { get; set; }
    public string? NotificationToken { get; set; }
}

}