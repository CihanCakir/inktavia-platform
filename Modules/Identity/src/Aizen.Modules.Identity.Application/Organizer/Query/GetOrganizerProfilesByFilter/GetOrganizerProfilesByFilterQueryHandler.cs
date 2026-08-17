using Aizen.Core.CQRS.Handler;
using Aizen.Core.Domain;
using Aizen.Core.UnitOfWork.Abstraction;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Enum;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;
using MiniUow.Paging;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Organizer;

public sealed class GetOrganizerProfilesByFilterQueryHandler
    : AizenQueryHandler<GetOrganizerProfilesByFilterQuery, IPaginate<OrganizerProfileListItemDto>>
{
    private readonly IAizenUnitOfWork<IdentityDbContext> _uow;
    private readonly IdentityDbContext _db;

    public GetOrganizerProfilesByFilterQueryHandler(IAizenUnitOfWork<IdentityDbContext> uow, IdentityDbContext db)
    {
        _uow = uow;
        _db = db;
    }

    public override async Task<IPaginate<OrganizerProfileListItemDto>> Handle(GetOrganizerProfilesByFilterQuery request, CancellationToken cancellationToken)
    {
        var repo = _uow.GetRepository<UserProfileEntity>();

        // EF Core cannot translate enum.ToString() inside WHERE — parse values before the lambda.
        ApprovalStatus? approvalFilter = Enum.TryParse<ApprovalStatus>(request.ApprovalStatus, ignoreCase: true, out var a) ? a : null;
        ProfileStatus? statusFilter = Enum.TryParse<ProfileStatus>(request.Status, ignoreCase: true, out var s) ? s : null;
        ProviderOnboardingStatus? onboardingFilter = Enum.TryParse<ProviderOnboardingStatus>(request.OnboardingStatus, ignoreCase: true, out var ob) ? ob : null;

        // When filtering by onboarding status, we need to pre-fetch matching profile IDs
        HashSet<long>? onboardingProfileIds = null;
        if (onboardingFilter != null)
        {
            onboardingProfileIds = (await _db.ProviderOnboarding
                .Where(o => !o.IsDeleted && o.Status == onboardingFilter)
                .Select(o => o.ProfileId)
                .ToListAsync(cancellationToken))
                .ToHashSet();
        }

        var pagedResult = await repo.GetPagedListAsync<OrganizerProfileListItemDto>(
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
                && (request.Country == null || (p.Country != null && p.Country.Contains(request.Country)))
                && (onboardingProfileIds == null || onboardingProfileIds.Contains(p.Id)),
            pageIndex: request.PageIndex,
            pageSize: request.PageSize,
            cancellationToken: cancellationToken);

        // Enrich with onboarding data and document counts
        var profileIds = pagedResult.Items.Select(x => x.Id).ToList();
        if (profileIds.Count > 0)
        {
            var onboardingMap = await _db.ProviderOnboarding
                .Where(o => profileIds.Contains(o.ProfileId) && !o.IsDeleted)
                .Select(o => new { o.ProfileId, o.Status, o.SubmittedAtUtc })
                .ToDictionaryAsync(o => o.ProfileId, cancellationToken);

            var docCountMap = await _db.VerificationDocuments
                .Where(d => profileIds.Contains(d.ProfileId) && !d.IsDeleted)
                .GroupBy(d => d.ProfileId)
                .Select(g => new { ProfileId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.ProfileId, x => x.Count, cancellationToken);

            foreach (var item in pagedResult.Items)
            {
                if (onboardingMap.TryGetValue(item.Id, out var onboarding))
                {
                    item.OnboardingStatus = onboarding.Status.ToString();
                    item.SubmittedAtUtc = onboarding.SubmittedAtUtc;
                }
                item.DocumentCount = docCountMap.GetValueOrDefault(item.Id, 0);
            }
        }

        return pagedResult;
    }
}
