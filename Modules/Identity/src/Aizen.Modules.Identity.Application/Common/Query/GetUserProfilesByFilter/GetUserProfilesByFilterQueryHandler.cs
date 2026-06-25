using Aizen.Core.CQRS.Handler;
using Aizen.Core.Domain;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;
using MiniUow.Paging;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Common;

public sealed class GetUserProfilesByFilterQueryHandler
    : AizenQueryHandler<GetUserProfilesByFilterQuery, IPaginate<UserProfileListItemDto>>
{
    private readonly IAizenUnitOfWork<IdentityDbContext> _uow;

    public GetUserProfilesByFilterQueryHandler(IAizenUnitOfWork<IdentityDbContext> uow)
    {
        _uow = uow;
    }

    public override async Task<IPaginate<UserProfileListItemDto>> Handle(GetUserProfilesByFilterQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<UserProfileEntity>();

        // EF Core cannot translate enum.ToString() inside WHERE — parse values before the lambda.
        WorkshopRoleContext? roleFilter = Enum.TryParse<WorkshopRoleContext>(request.RoleContext, out var r) ? r : null;
        ApprovalStatus? approvalFilter = Enum.TryParse<ApprovalStatus>(request.ApprovalStatus, out var a) ? a : null;
        ProfileStatus? statusFilter = Enum.TryParse<ProfileStatus>(request.Status, out var s) ? s : null;

        return await repo.GetPagedListAsync<UserProfileListItemDto>(
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
                Email = p.User != null ? p.User.Email : null,
                PhoneNumber = p.User != null ? p.User.PhoneNumber : null,
                CreateDate = p.CreateDate,
                LastLoginAt = p.User != null && p.User.UserLoginTokens != null
                    ? p.User.UserLoginTokens
                        .Where(t => !t.IsRevoked)
                        .Max(t => (DateTime?)(t.ModifyDate ?? t.CreateDate))
                    : null
            },
            predicate: p => !p.IsDeleted
                && (request.FirstName == null || p.FirstName.Contains(request.FirstName))
                && (request.LastName == null || p.LastName.Contains(request.LastName))
                && (roleFilter == null || p.RoleContext == roleFilter)
                && (approvalFilter == null || p.ApprovalStatus == approvalFilter)
                && (statusFilter == null || p.Status == statusFilter)
                && (request.Email == null || (p.User != null && p.User.Email != null && p.User.Email.Contains(request.Email))),
            pageIndex: request.PageIndex,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);
    }
}
