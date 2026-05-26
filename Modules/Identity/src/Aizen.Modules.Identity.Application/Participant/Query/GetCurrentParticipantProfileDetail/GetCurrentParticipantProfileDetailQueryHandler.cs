using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Participant;

public sealed class GetCurrentParticipantProfileDetailQueryHandler
    : AizenQueryHandler<GetCurrentParticipantProfileDetailQuery, ParticipantProfileDetailDto>
{
    private readonly IAizenInfoAccessor _info;
    private readonly IdentityDbContext _db;

    public GetCurrentParticipantProfileDetailQueryHandler(IAizenInfoAccessor info, IdentityDbContext db)
    {
        _info = info;
        _db = db;
    }

    public override async Task<ParticipantProfileDetailDto> Handle(GetCurrentParticipantProfileDetailQuery request, CancellationToken cancellationToken)
    {
        var userId = _info.UserInfoAccessor.UserInfo.UserId;

        var dto = await _db.UserProfiles
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.RoleContext == WorkshopRoleContext.Participant && !p.IsDeleted)
            .Select(p => new ParticipantProfileDetailDto
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
