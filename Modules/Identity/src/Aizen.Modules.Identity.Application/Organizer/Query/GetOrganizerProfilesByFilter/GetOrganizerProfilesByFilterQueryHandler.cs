using Aizen.Core.CQRS.Handler;
using Aizen.Core.Domain;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Repository.Context;
using MiniUow.Paging;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Organizer;

public sealed class GetOrganizerProfilesByFilterQueryHandler
    : AizenQueryHandler<GetOrganizerProfilesByFilterQuery, IPaginate<OrganizerProfileListItemDto>>
{
    private readonly IAizenUnitOfWork<IdentityDbContext> _uow;

    public GetOrganizerProfilesByFilterQueryHandler(IAizenUnitOfWork<IdentityDbContext> uow)
    {
        _uow = uow;
    }

    public override async Task<IPaginate<OrganizerProfileListItemDto>> Handle(GetOrganizerProfilesByFilterQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<UserProfileEntity>();

        // EF Core cannot translate enum.ToString() inside WHERE — parse values before the lambda.
        ApprovalStatus? approvalFilter = Enum.TryParse<ApprovalStatus>(request.ApprovalStatus, out var a) ? a : null;
        ProfileStatus? statusFilter = Enum.TryParse<ProfileStatus>(request.Status, out var s) ? s : null;

        return await repo.GetPagedListAsync<OrganizerProfileListItemDto>(
            selector: p => new OrganizerProfileListItemDto
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
                    : null,
                OrganizationName = p.CompanyName,
                Email = p.User != null ? p.User.Email : null,
                Phone = p.User != null ? p.User.PhoneNumber : null,
                City = p.City,
                Country = p.Country,
                ReviewedAt = p.ReviewedAt != null ? p.ReviewedAt.Value.ToString("O") : null,
                RiskLevel = p.RiskSignals.Any(rs => rs.Severity == "high") ? "H"
                          : p.RiskSignals.Any(rs => rs.Severity == "medium") ? "M"
                          : "L"
            },
            predicate: p => p.RoleContext == WorkshopRoleContext.Organizer && !p.IsDeleted
                && (request.FirstName == null || p.FirstName.Contains(request.FirstName))
                && (request.LastName == null || p.LastName.Contains(request.LastName))
                && (approvalFilter == null || p.ApprovalStatus == approvalFilter)
                && (statusFilter == null || p.Status == statusFilter)
                && (request.SearchTerm == null
                    || p.FirstName.Contains(request.SearchTerm)
                    || p.LastName.Contains(request.SearchTerm)
                    || (p.CompanyName != null && p.CompanyName.Contains(request.SearchTerm)))
                && (request.City == null || (p.City != null && p.City.Contains(request.City)))
                && (request.Country == null || (p.Country != null && p.Country.Contains(request.Country))),
            pageIndex: request.PageIndex,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);
    }
}
