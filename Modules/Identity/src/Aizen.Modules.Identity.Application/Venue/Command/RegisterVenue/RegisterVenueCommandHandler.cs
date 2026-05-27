using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Enum;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.InktaviaStore.Application.Identity.Command.RegisterVenue;


namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Registration
{
    public class RegisterVenueCommandHandler : AizenCommandHandler<RegisterVenueCommand, RegisterResult>
    {
        private readonly IVenueRegistrationDomainService _domain;

        public RegisterVenueCommandHandler(IVenueRegistrationDomainService domain)
            => _domain = domain;

        public override async Task<RegisterResult?> Handle(RegisterVenueCommand request, CancellationToken ct)
        {
            var (user, profile) = await _domain.RegisterOrAttachAsync(new RegisterVenueDomainModel
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
                ActiveProfileId: null,
                AccessToken: null,
                RefreshToken: null,
                Message: "Venue profile submitted for approval."
            );
        }
    }
}