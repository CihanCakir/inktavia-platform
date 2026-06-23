using Aizen.Core.CQRS.Handler;
using Aizen.Core.Domain;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Organizer;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Organizer;

public sealed class GetOrganizerProfileListQueryHandler
    : AizenQueryHandler<GetOrganizerProfileListQuery, IList<OrganizerProfileListItemDto>>
{
    private readonly IdentityDbContext _db;

    public GetOrganizerProfileListQueryHandler(IdentityDbContext db)
    {
        _db = db;
    }

    public override async Task<IList<OrganizerProfileListItemDto>> Handle(GetOrganizerProfileListQuery request, CancellationToken cancellationToken)
    {
        ApprovalStatus? approvalFilter = Enum.TryParse<ApprovalStatus>(request.ApprovalStatus, out var a) ? a : null;

        var items = await _db.UserProfiles
            .AsNoTracking()
            .Where(p => p.RoleContext == WorkshopRoleContext.Organizer && !p.IsDeleted
                && (request.FirstName == null || p.FirstName.Contains(request.FirstName))
                && (request.LastName == null || p.LastName.Contains(request.LastName))
                && (approvalFilter == null || p.ApprovalStatus == approvalFilter))
            .Select(p => new OrganizerProfileListItemDto
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
                OrganizationName = p.CompanyName,
                Email = p.User != null ? p.User.Email : null,
                Phone = p.User != null ? p.User.PhoneNumber : null,
                City = p.City,
                Country = p.Country,
                ReviewedAt = p.ReviewedAt != null ? p.ReviewedAt.Value.ToString("O") : null,
                RiskLevel = p.RiskSignals.Any(s => s.Severity == "high") ? "H"
                          : p.RiskSignals.Any(s => s.Severity == "medium") ? "M"
                          : "L"
            })
            .ToListAsync(cancellationToken);

        return items;
    }
}
