using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Common;

public sealed class GetUserProfileDetailQueryHandler
    : AizenQueryHandler<GetUserProfileDetailQuery, UserProfileDetailDto>
{
    private readonly IdentityDbContext _db;

    public GetUserProfileDetailQueryHandler(IdentityDbContext db)
    {
        _db = db;
    }

    public override async Task<UserProfileDetailDto> Handle(GetUserProfileDetailQuery request, CancellationToken cancellationToken)
    {
        var dto = await _db.UserProfiles
            .AsNoTracking()
            .Where(p => p.Id == request.ProfileId && !p.IsDeleted)
            .Select(p => new UserProfileDetailDto
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
                RoleContext = p.RoleContext.ToString(),
                ApprovalStatus = p.ApprovalStatus.ToString(),
                Status = p.Status.ToString(),
                ApprovedAt = p.ApprovedAt,
                RejectedAt = p.RejectedAt,
                RejectReason = p.RejectReason,
                TaxpayerType = p.TaxpayerType.ToString(),
                CreateDate = p.CreateDate,
                ModifyDate = p.ModifyDate
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AizenBusinessException(((int)AizenErrorCode.NotFound).ToString());

        return dto;
    }
}
