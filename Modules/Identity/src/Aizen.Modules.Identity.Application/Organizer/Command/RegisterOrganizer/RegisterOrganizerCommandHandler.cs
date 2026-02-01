using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Enum;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Domain.Interface.Service;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.RegisterOrganizer
{
    public class RegisterOrganizerCommandHandler : AizenCommandHandler<RegisterOrganizerCommand, RegisterResult>
    {
        private readonly IOrganizerRegistrationDomainService _domain;

        public RegisterOrganizerCommandHandler(IOrganizerRegistrationDomainService domain)
            => _domain = domain;

        public override async Task<RegisterResult?> Handle(RegisterOrganizerCommand request, CancellationToken ct)
        {
            var (user, profile) = await _domain.RegisterOrAttachAsync(new RegisterOrganizerDomainModel
            {
                Email = request.Email,
                Phone = request.ContactPhone,
                Password = request.Password,
                OwnerFirstName = request.OwnerFirstName,
                OwnerLastName = request.OwnerLastName,
                KvkkAccepted = request.KvkkAccepted,
                DeviceId = request.DeviceId,
                DeviceType = request.DeviceType,
                NotificationToken = request.NotificationToken
            }, ct);

            return new RegisterResult(
                Success: true,
                Status: RegistrationStatus.PendingApproval,
                UserId: user.Id,
                ActiveProfileId: null,       // onayda aktif olacak
                AccessToken: null,
                RefreshToken: null,
                Message: "Organizer profile submitted for approval."
            );
        }
    }
}