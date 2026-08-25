using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;

namespace Aizen.Modules.Identity.Application.ParticipantLookup.GetProfilePhoneNumber;

public sealed class GetProfilePhoneNumberByProfileIdQueryHandler
    : AizenQueryHandler<GetProfilePhoneNumberByProfileIdQuery, ProfilePhoneNumberDto>
{
    private readonly IAizenUnitOfWork<IdentityDbContext> _uow;

    public GetProfilePhoneNumberByProfileIdQueryHandler(IAizenUnitOfWork<IdentityDbContext> uow) => _uow = uow;

    public override async Task<ProfilePhoneNumberDto?> Handle(
        GetProfilePhoneNumberByProfileIdQuery request, CancellationToken cancellationToken)
    {
        if (request.ProfileId <= 0)
            return new ProfilePhoneNumberDto { ProfileId = request.ProfileId, PhoneNumber = null };

        // Telefon UserEntity.PhoneNumber'da (IdentityUser<long>'dan miras); UserProfiles→Users üzerinden — e-posta ile aynı yol.
        var repo = _uow.GetRepository<UserProfileEntity>();
        var row = (await repo.GetAllAsync<PhoneRow>(
            selector: p => new PhoneRow { ProfileId = p.Id, PhoneNumber = p.User.PhoneNumber },
            predicate: p => !p.IsDeleted && p.Id == request.ProfileId)).FirstOrDefault();

        return new ProfilePhoneNumberDto { ProfileId = request.ProfileId, PhoneNumber = row?.PhoneNumber };
    }

    private sealed class PhoneRow
    {
        public long    ProfileId   { get; set; }
        public string? PhoneNumber { get; set; }
    }
}
