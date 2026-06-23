using Aizen.Core.CQRS.Handler;
using Aizen.Core.Domain;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Venue;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;
using MiniUow.Paging;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Venue;

public sealed class GetVenueProfilesByFilterQueryHandler
    : AizenQueryHandler<GetVenueProfilesByFilterQuery, IPaginate<VenueProfileListItemDto>>
{
    private readonly IAizenUnitOfWork<IdentityDbContext> _uow;

    public GetVenueProfilesByFilterQueryHandler(IAizenUnitOfWork<IdentityDbContext> uow)
    {
        _uow = uow;
    }

    public override async Task<IPaginate<VenueProfileListItemDto>> Handle(GetVenueProfilesByFilterQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<UserProfileEntity>();

        // EF Core cannot translate enum.ToString() inside WHERE — parse values before the lambda.
        ApprovalStatus? approvalFilter = Enum.TryParse<ApprovalStatus>(request.ApprovalStatus, out var a) ? a : null;

        return await repo.GetPagedListAsync<VenueProfileListItemDto>(
            selector: p => new VenueProfileListItemDto
            {
                Id = p.Id,
                UserId = p.UserId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                ProfilePhotoUrl = p.ProfilePhotoUrl,
                TaxpayerType = p.TaxpayerType.ToString(),
                ApprovalStatus = p.ApprovalStatus.ToString(),
                Status = p.Status.ToString(),
                CreateDate = p.CreateDate,
                LastLoginAt = p.User != null && p.User.UserLoginTokens != null
                    ? p.User.UserLoginTokens
                        .Where(t => !t.IsRevoked)
                        .Max(t => (DateTime?)(t.ModifyDate ?? t.CreateDate))
                    : null
            },
            predicate: p => p.RoleContext == WorkshopRoleContext.VenueOwner && !p.IsDeleted
                && (request.FirstName == null || p.FirstName.Contains(request.FirstName))
                && (request.LastName == null || p.LastName.Contains(request.LastName))
                && (approvalFilter == null || p.ApprovalStatus == approvalFilter),
            pageIndex: request.PageIndex,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);
    }
}
