using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Identity.Abstraction.Request
{
    public sealed class RegisterConsumerRequest
    {
        public string Email { get; set; } = default!;
        public string? Phone { get; set; }
        public string Password { get; set; } = default!;
        public string FirstName { get; set; } = default!;
        public string LastName { get; set; } = default!;
        public bool KvkkAccepted { get; set; }

        public string? DeviceId { get; set; }
        public ConsumerDeviceType? DeviceType { get; set; }
        public string? NotificationToken { get; set; }
    }
}