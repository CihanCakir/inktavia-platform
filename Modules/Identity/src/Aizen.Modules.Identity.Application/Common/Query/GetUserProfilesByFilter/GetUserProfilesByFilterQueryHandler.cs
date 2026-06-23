using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;
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
                CreateDate = p.CreateDate
            },
            predicate: p => !p.IsDeleted
                && (request.FirstName == null || p.FirstName.Contains(request.FirstName))
                && (request.LastName == null || p.LastName.Contains(request.LastName))
                && (request.RoleContext == null || p.RoleContext.ToString() == request.RoleContext)
                && (request.ApprovalStatus == null || p.ApprovalStatus.ToString() == request.ApprovalStatus)
                && (request.Status == null || p.Status.ToString() == request.Status)
                && (request.Email == null || (p.User != null && p.User.Email != null && p.User.Email.Contains(request.Email))),
            pageIndex: request.PageIndex,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);
    }
}
