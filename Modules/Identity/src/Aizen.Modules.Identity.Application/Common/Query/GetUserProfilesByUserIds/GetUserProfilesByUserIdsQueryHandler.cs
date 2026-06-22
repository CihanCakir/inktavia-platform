using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Common;

/// <summary>Returns a flat list of user profiles matching the given user IDs. Used for BFF bulk owner enrichment.</summary>
public sealed class GetUserProfilesByUserIdsQueryHandler
    : AizenQueryHandler<GetUserProfilesByUserIdsQuery, IList<UserProfileListItemDto>>
{
    private readonly IAizenUnitOfWork<IdentityDbContext> _uow;

    public GetUserProfilesByUserIdsQueryHandler(IAizenUnitOfWork<IdentityDbContext> uow)
    {
        _uow = uow;
    }

    public override async Task<IList<UserProfileListItemDto>> Handle(
        GetUserProfilesByUserIdsQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<UserProfileEntity>();

        var all = await repo.GetAllAsync<UserProfileListItemDto>(
            selector: p => new UserProfileListItemDto
            {
                Id = p.Id,
                UserId = p.UserId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                ProfilePhotoUrl = p.ProfilePhotoUrl,
                RoleContext = p.RoleContext.ToString(),
                ApprovalStatus = p.ApprovalStatus.ToString(),
                Status = p.Status.ToString(),
                CreateDate = p.CreateDate
            },
            predicate: p => !p.IsDeleted && request.UserIds.Contains(p.UserId));

        return all.ToList();
    }
}
