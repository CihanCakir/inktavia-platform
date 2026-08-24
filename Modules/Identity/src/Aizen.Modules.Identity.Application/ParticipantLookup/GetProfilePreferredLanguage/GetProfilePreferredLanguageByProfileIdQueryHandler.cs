using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;

namespace Aizen.Modules.Identity.Application.ParticipantLookup.GetProfilePreferredLanguage;

/// <summary>
/// Reads the linked user's preferred language for one profile id in a single query (projects the language column only
/// — nothing else about the profile or user crosses). Mirrors the contact-email lookup's UserProfiles→Users path.
/// </summary>
public sealed class GetProfilePreferredLanguageByProfileIdQueryHandler
    : AizenQueryHandler<GetProfilePreferredLanguageByProfileIdQuery, ProfilePreferredLanguageDto>
{
    private readonly IAizenUnitOfWork<IdentityDbContext> _uow;

    public GetProfilePreferredLanguageByProfileIdQueryHandler(IAizenUnitOfWork<IdentityDbContext> uow) => _uow = uow;

    public override async Task<ProfilePreferredLanguageDto?> Handle(
        GetProfilePreferredLanguageByProfileIdQuery request, CancellationToken cancellationToken)
    {
        if (request.ProfileId <= 0)
            return new ProfilePreferredLanguageDto { ProfileId = request.ProfileId, PreferredLanguage = null };

        var repo = _uow.GetRepository<UserProfileEntity>();
        var row = (await repo.GetAllAsync<LanguageRow>(
            selector: p => new LanguageRow { ProfileId = p.Id, PreferredLanguage = p.User.PreferredLanguage },
            predicate: p => !p.IsDeleted && p.Id == request.ProfileId)).FirstOrDefault();

        return new ProfilePreferredLanguageDto { ProfileId = request.ProfileId, PreferredLanguage = row?.PreferredLanguage };
    }

    private sealed class LanguageRow
    {
        public long ProfileId { get; set; }
        public string? PreferredLanguage { get; set; }
    }
}
