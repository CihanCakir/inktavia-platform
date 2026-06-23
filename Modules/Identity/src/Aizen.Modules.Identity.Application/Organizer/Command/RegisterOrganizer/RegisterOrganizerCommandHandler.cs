using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto;
using Aizen.Modules.Identity.Abstraction.Enum;
using Aizen.Modules.Identity.Abstraction.Model;
using Aizen.Modules.Identity.Domain.Interface.Service;
using Aizen.Modules.Identity.Domain.Service;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.RegisterOrganizer
{
    public class RegisterOrganizerCommandHandler : AizenCommandHandler<RegisterOrganizerCommand, RegisterResult>
    {
        private readonly IOrganizerRegistrationDomainService _domain;
        private readonly IdentityDbContext _db;

        public RegisterOrganizerCommandHandler(IOrganizerRegistrationDomainService domain, IdentityDbContext db)
        {
            _domain = domain;
            _db = db;
        }

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

            // Generate risk signals for the new pending profile
            var hasDuplicateEmail = !string.IsNullOrWhiteSpace(user.Email)
                && await _db.UserProfiles
                    .AnyAsync(p => p.Id != profile.Id && !p.IsDeleted
                        && p.User.Email == user.Email, ct);

            var signals = RiskAssessmentService.Evaluate(
                profileId: profile.Id,
                email: user.Email,
                phone: user.PhoneNumber,
                companyName: profile.CompanyName,
                hasDuplicateEmail: hasDuplicateEmail,
                documentCount: 0);

            if (signals.Count > 0)
            {
                await _db.RiskSignals.AddRangeAsync(signals, ct);
                await _db.SaveChangesAsync(ct);
            }

            return new RegisterResult(
                Success: true,
                Status: RegistrationStatus.PendingApproval,
                UserId: user.Id,
                ActiveProfileId: null,
                AccessToken: null,
                RefreshToken: null,
                Message: "Organizer profile submitted for approval."
            );
        }
    }
}
