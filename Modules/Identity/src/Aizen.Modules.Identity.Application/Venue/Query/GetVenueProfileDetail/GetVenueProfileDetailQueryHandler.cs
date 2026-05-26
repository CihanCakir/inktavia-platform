using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Venue;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Venue;

public sealed class GetVenueProfileDetailQueryHandler
    : AizenQueryHandler<GetVenueProfileDetailQuery, VenueProfileDetailDto>
{
    private readonly IdentityDbContext _db;

    public GetVenueProfileDetailQueryHandler(IdentityDbContext db)
    {
        _db = db;
    }

    public override async Task<VenueProfileDetailDto> Handle(GetVenueProfileDetailQuery request, CancellationToken cancellationToken)
    {
        var dto = await _db.UserProfiles
            .AsNoTracking()
            .Where(p => p.Id == request.ProfileId && p.RoleContext == WorkshopRoleContext.VenueOwner && !p.IsDeleted)
            .Select(p => new VenueProfileDetailDto
            {
                Id = p.Id,
                UserId = p.UserId,
                FirstName = p.FirstName,
                LastName = p.LastName,
                Gender = p.Gender,
                BirthDate = p.BirthDate,
                Bio = p.Bio,
                ProfilePhotoUrl = p.ProfilePhotoUrl,
                NationalityId = p.NationalityId,
                TaxpayerType = p.TaxpayerType.ToString(),
                ApprovalStatus = p.ApprovalStatus.ToString(),
                Status = p.Status.ToString(),
                ApprovedAt = p.ApprovedAt,
                RejectedAt = p.RejectedAt,
                RejectReason = p.RejectReason,
                CreateDate = p.CreateDate,
                ModifyDate = p.ModifyDate
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AizenBusinessException(((int)AizenErrorCode.NotFound).ToString());

        return dto;
    }
}
