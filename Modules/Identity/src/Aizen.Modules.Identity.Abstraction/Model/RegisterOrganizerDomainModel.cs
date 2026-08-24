using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Identity.Abstraction.Model
{
    // Domain/Models/RegisterOrganizerDomainModel.cs
    public sealed class RegisterOrganizerDomainModel
    {
        public string Email { get; init; } = default!;
        public string? Phone { get; init; }
        public string Password { get; init; } = default!;
        public string OwnerFirstName { get; init; } = default!;
        public string OwnerLastName { get; init; } = default!;
        public bool KvkkAccepted { get; init; }

        public string? DeviceId { get; init; }
        public ConsumerDeviceType? DeviceType { get; init; }
        public string? NotificationToken { get; init; }

        /// <summary>Kayıt anında ClientInfo.Language'den (Accept-Language) gelen tercih; UserEntity normalize/doğrular.</summary>
        public string? PreferredLanguage { get; init; }

        public string[] RequiredAgreementTypes { get; init; } = new[] { "KVKK", "TERMS_OF_USE", "ORGANIZER_SPECIFIC_TERMS" };
    }

}