using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;

namespace Aizen.Modules.Identity.Application.AdminUsers.GetAllProviderProfileIds;

public sealed class GetAllProviderProfileIdsQueryHandler
    : AizenQueryHandler<GetAllProviderProfileIdsQuery, IList<long>>
{
    private readonly IAizenUnitOfWork<IdentityDbContext> _uow;

    public GetAllProviderProfileIdsQueryHandler(IAizenUnitOfWork<IdentityDbContext> uow) => _uow = uow;

    private sealed class Row { public long ProfileId { get; init; } }

    public override async Task<IList<long>> Handle(GetAllProviderProfileIdsQuery request, CancellationToken ct)
    {
        var repo = _uow.GetRepository<UserProfileEntity>();
        // Sağlayıcı = Organizer + VenueOwner. Profil id'si (p.Id) seçilir — Notification satırları profil id'sine dosyalanır.
        var rows = await repo.GetAllAsync<Row>(
            selector: p => new Row { ProfileId = p.Id },
            predicate: p => (p.RoleContext == WorkshopRoleContext.Organizer
                             || p.RoleContext == WorkshopRoleContext.VenueOwner) && !p.IsDeleted);

        return rows.Select(r => r.ProfileId).Distinct().ToList();
    }
}
