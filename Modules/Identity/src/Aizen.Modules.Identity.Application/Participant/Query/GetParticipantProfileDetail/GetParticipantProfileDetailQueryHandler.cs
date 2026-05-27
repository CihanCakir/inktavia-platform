using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Participant;

public sealed class GetParticipantProfileDetailQueryHandler
    : AizenQueryHandler<GetParticipantProfileDetailQuery, ParticipantProfileDetailDto>
{
    private readonly IdentityDbContext _db;

    public GetParticipantProfileDetailQueryHandler(IdentityDbContext db)
    {
        _db = db;
    }

    public override async Task<ParticipantProfileDetailDto> Handle(GetParticipantProfileDetailQuery request, CancellationToken cancellationToken)
    {
        var dto = await _db.UserProfiles
            .AsNoTracking()
            .Where(p => p.Id == request.ProfileId && p.RoleContext == WorkshopRoleContext.Participant && !p.IsDeleted)
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
