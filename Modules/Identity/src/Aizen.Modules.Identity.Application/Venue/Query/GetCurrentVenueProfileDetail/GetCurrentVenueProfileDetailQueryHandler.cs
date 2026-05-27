using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Venue;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Venue;

public sealed class GetCurrentVenueProfileDetailQueryHandler
    : AizenQueryHandler<GetCurrentVenueProfileDetailQuery, VenueProfileDetailDto>
{
    private readonly IAizenInfoAccessor _info;
    private readonly IdentityDbContext _db;

    public GetCurrentVenueProfileDetailQueryHandler(IAizenInfoAccessor info, IdentityDbContext db)
    {
        _info = info;
        _db = db;
    }

    public override async Task<VenueProfileDetailDto> Handle(GetCurrentVenueProfileDetailQuery request, CancellationToken cancellationToken)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;

        var dto = await _db.UserProfiles
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.RoleContext == WorkshopRoleContext.VenueOwner && !p.IsDeleted)
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
