using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;

namespace Aizen.Modules.Identity.Application.AdminUsers.GetAdminUserIds;

public sealed class GetAdminUserIdsQueryHandler
    : AizenQueryHandler<GetAdminUserIdsQuery, IList<long>>
{
    private readonly IAizenUnitOfWork<IdentityDbContext> _uow;

    public GetAdminUserIdsQueryHandler(IAizenUnitOfWork<IdentityDbContext> uow) => _uow = uow;

    private sealed class Row { public long UserId { get; init; } }

    public override async Task<IList<long>> Handle(GetAdminUserIdsQuery request, CancellationToken ct)
    {
        var repo = _uow.GetRepository<UserProfileEntity>();
        var rows = await repo.GetAllAsync<Row>(
            selector: p => new Row { UserId = p.UserId },
            predicate: p => p.RoleContext == WorkshopRoleContext.Admin && !p.IsDeleted);

        return rows.Select(r => r.UserId).Distinct().ToList();
    }
}
