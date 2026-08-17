using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Command.Organizer.MarkOrganizerPhoneVerified;

/// <summary>
/// Marks an Organizer profile's phone as verified. Called by the MarineProvider BFF only after a
/// successful OTP check (service-token authorized). Uses the unit of work / repository (no direct DbContext).
/// Returns PhoneVerified=false when the profile is not found so callers can surface a controlled result.
/// </summary>
public sealed class MarkOrganizerPhoneVerifiedCommandHandler
    : AizenCommandHandler<MarkOrganizerPhoneVerifiedCommand, MarkOrganizerPhoneVerifiedResult>
{
    private readonly IAizenUnitOfWork<IdentityDbContext> _uow;

    public MarkOrganizerPhoneVerifiedCommandHandler(IAizenUnitOfWork<IdentityDbContext> uow)
    {
        _uow = uow;
    }

    public override async Task<MarkOrganizerPhoneVerifiedResult?> Handle(
        MarkOrganizerPhoneVerifiedCommand request, CancellationToken cancellationToken)
    {
        var profileRepo = _uow.GetRepository<UserProfileEntity>();

        var profile = await profileRepo.FirstOrDefaultAsync(
            p => p.Id == request.ProfileId
                 && p.RoleContext == WorkshopRoleContext.Organizer
                 && !p.IsDeleted);

        if (profile is null)
            return new MarkOrganizerPhoneVerifiedResult(request.ProfileId, false);

        profile.MarkPhoneVerified(DateTime.UtcNow);
        profileRepo.Update(profile);

        return new MarkOrganizerPhoneVerifiedResult(profile.Id, true);
    }
}
