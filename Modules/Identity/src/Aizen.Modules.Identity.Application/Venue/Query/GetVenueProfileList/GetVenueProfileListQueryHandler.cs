using Aizen.Core.CQRS.Handler;
using Aizen.Core.Domain;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Venue;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Venue;

public sealed class GetVenueProfileListQueryHandler
    : AizenQueryHandler<GetVenueProfileListQuery, IList<VenueProfileListItemDto>>
{
    private readonly IdentityDbContext _db;

    public GetVenueProfileListQueryHandler(IdentityDbContext db)
    {
        _db = db;
    }

    public override async Task<IList<VenueProfileListItemDto>> Handle(GetVenueProfileListQuery request, CancellationToken cancellationToken)
    {
        ApprovalStatus? approvalFilter = Enum.TryParse<ApprovalStatus>(request.ApprovalStatus, out var a) ? a : null;

        var items = await _db.UserProfiles
            .AsNoTracking()
            .Where(p => p.RoleContext == WorkshopRoleContext.VenueOwner && !p.IsDeleted
                && (request.FirstName == null || p.FirstName.Contains(request.FirstName))
                && (request.LastName == null || p.LastName.Contains(request.LastName))
                && (approvalFilter == null || p.ApprovalStatus == approvalFilter))
            .Select(p => new VenueProfileListItemDto
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
                VenueName = p.CompanyName,
                OwnerName = p.FirstName + " " + p.LastName,
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
