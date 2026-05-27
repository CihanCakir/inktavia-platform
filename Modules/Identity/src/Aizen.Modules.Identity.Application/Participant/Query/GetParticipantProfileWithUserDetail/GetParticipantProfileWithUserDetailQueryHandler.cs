using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction;
using Aizen.Modules.Identity.Abstraction.Dto.Participant;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Participant;

public sealed class GetParticipantProfileWithUserDetailQueryHandler
    : AizenQueryHandler<GetParticipantProfileWithUserDetailQuery, ParticipantProfileWithUserDetailDto>
{
    private readonly IdentityDbContext _db;

    public GetParticipantProfileWithUserDetailQueryHandler(IdentityDbContext db)
    {
        _db = db;
    }

    public override async Task<ParticipantProfileWithUserDetailDto> Handle(GetParticipantProfileWithUserDetailQuery request, CancellationToken cancellationToken)
    {
        var profile = await _db.UserProfiles
            .AsNoTracking()
            .Include(p => p.User)
            .Where(p => p.Id == request.ProfileId && p.RoleContext == WorkshopRoleContext.Participant && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AizenBusinessException(((int)AizenErrorCode.NotFound).ToString());

        return new ParticipantProfileWithUserDetailDto
        {
            Id = profile.Id,
            UserId = profile.UserId,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Email = profile.User?.Email,
            PhoneNumber = profile.User?.PhoneNumber,
            Gender = profile.Gender,
            BirthDate = profile.BirthDate,
            Bio = profile.Bio,
            ProfilePhotoUrl = profile.ProfilePhotoUrl,
            NationalityId = profile.NationalityId,
            ApprovalStatus = profile.ApprovalStatus.ToString(),
            Status = profile.Status.ToString(),
            ApprovedAt = profile.ApprovedAt,
            LoginType = profile.User?.LoginType.ToString(),
            UserCreatedAt = profile.User?.CreatedAt ?? profile.CreateDate,
            CreateDate = profile.CreateDate
        };
    }
}
