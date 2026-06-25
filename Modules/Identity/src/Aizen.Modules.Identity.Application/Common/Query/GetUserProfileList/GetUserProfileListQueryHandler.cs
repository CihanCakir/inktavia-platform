using Aizen.Core.CQRS.Handler;
using Aizen.Core.Domain;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Common;

public sealed class GetUserProfileListQueryHandler
    : AizenQueryHandler<GetUserProfileListQuery, IList<UserProfileListItemDto>>
{
    private readonly IAizenUnitOfWork<IdentityDbContext> _uow;

    public GetUserProfileListQueryHandler(IAizenUnitOfWork<IdentityDbContext> uow)
    {
        _uow = uow;
    }

    public override async Task<IList<UserProfileListItemDto>> Handle(GetUserProfileListQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<UserProfileEntity>();

        // EF Core cannot translate enum.ToString() inside WHERE — parse values before the lambda.
        WorkshopRoleContext? roleFilter = Enum.TryParse<WorkshopRoleContext>(request.RoleContext, out var r) ? r : null;
        ApprovalStatus? approvalFilter = Enum.TryParse<ApprovalStatus>(request.ApprovalStatus, out var a) ? a : null;

        var queryable = await repo.GetAllAsync<UserProfileListItemDto>(
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
            predicate: p => !p.IsDeleted
                && (request.FirstName == null || p.FirstName.Contains(request.FirstName))
                && (request.LastName == null || p.LastName.Contains(request.LastName))
                && (roleFilter == null || p.RoleContext == roleFilter)
                && (approvalFilter == null || p.ApprovalStatus == approvalFilter));

        return queryable.ToList();
    }
}
