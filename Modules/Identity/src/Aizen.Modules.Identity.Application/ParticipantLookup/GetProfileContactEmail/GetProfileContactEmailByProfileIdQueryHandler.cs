using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;

namespace Aizen.Modules.Identity.Application.ParticipantLookup.GetProfileContactEmail;

/// <summary>
/// Reads the linked user's email for one profile id in a single query (projects the email column only — nothing else
/// about the profile or user crosses).
/// </summary>
public sealed class GetProfileContactEmailByProfileIdQueryHandler
    : AizenQueryHandler<GetProfileContactEmailByProfileIdQuery, ProfileContactEmailDto>
{
    private readonly IAizenUnitOfWork<IdentityDbContext> _uow;

    public GetProfileContactEmailByProfileIdQueryHandler(IAizenUnitOfWork<IdentityDbContext> uow) => _uow = uow;

    public override async Task<ProfileContactEmailDto?> Handle(
        GetProfileContactEmailByProfileIdQuery request, CancellationToken cancellationToken)
    {
        if (request.ProfileId <= 0)
            return new ProfileContactEmailDto { ProfileId = request.ProfileId, Email = null };

        var repo = _uow.GetRepository<UserProfileEntity>();
        var row = (await repo.GetAllAsync<EmailRow>(
            selector: p => new EmailRow { ProfileId = p.Id, Email = p.User.Email },
            predicate: p => !p.IsDeleted && p.Id == request.ProfileId)).FirstOrDefault();

        return new ProfileContactEmailDto { ProfileId = request.ProfileId, Email = row?.Email };
    }

    private sealed class EmailRow
    {
        public long ProfileId { get; set; }
        public string? Email { get; set; }
    }
}
