using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Aizen.Modules.Identity.Abstraction.Model
{
    public sealed class RegisterParticipantDomainModel
    {
        public string Email { get; init; } = default!;
        public string? Phone { get; init; }
        public string Password { get; init; } = default!;
        public string FirstName { get; init; } = default!;
        public string LastName { get; init; } = default!;
        public bool KvkkAccepted { get; init; }

        public string? DeviceId { get; init; }
        public ConsumerDeviceType? DeviceType { get; init; }
        public string? NotificationToken { get; init; }

        public WorkshopRoleContext RoleContext { get; init; } = WorkshopRoleContext.Participant;

        // İsteğe bağlı: hangi sözleşme tiplerini zorunlu sayıyoruz?
        public string[] RequiredAgreementTypes { get; init; } = new[] { "KVKK", "TERMS_OF_USE", "WORKSHOP_PARTICIPANT_TERMS" };
    }

}