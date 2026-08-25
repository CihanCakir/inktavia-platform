using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;

namespace Aizen.Modules.Identity.Application.AdminUsers.GetAllParticipantProfileIds;

public sealed class GetAllParticipantProfileIdsQueryHandler
    : AizenQueryHandler<GetAllParticipantProfileIdsQuery, IList<long>>
{
    private readonly IAizenUnitOfWork<IdentityDbContext> _uow;

    public GetAllParticipantProfileIdsQueryHandler(IAizenUnitOfWork<IdentityDbContext> uow) => _uow = uow;

    private sealed class Row { public long ProfileId { get; init; } }

    public override async Task<IList<long>> Handle(GetAllParticipantProfileIdsQuery request, CancellationToken ct)
    {
        var repo = _uow.GetRepository<UserProfileEntity>();
        // Katılımcı profil id'si (p.Id) seçilir — owner-facing satırlar profil id'sine dosyalanır (NF1b).
        var rows = await repo.GetAllAsync<Row>(
            selector: p => new Row { ProfileId = p.Id },
            predicate: p => p.RoleContext == WorkshopRoleContext.Participant && !p.IsDeleted);

        return rows.Select(r => r.ProfileId).Distinct().ToList();
    }
}
