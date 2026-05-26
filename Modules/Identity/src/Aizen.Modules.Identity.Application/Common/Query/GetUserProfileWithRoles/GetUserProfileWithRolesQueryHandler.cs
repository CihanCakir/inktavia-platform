using Aizen.Core.Api.Middleware;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Common;

public sealed class GetUserProfileWithRolesQueryHandler
    : AizenQueryHandler<GetUserProfileWithRolesQuery, UserProfileWithRolesDto>
{
    private readonly IdentityDbContext _db;

    public GetUserProfileWithRolesQueryHandler(IdentityDbContext db)
    {
        _db = db;
    }

    public override async Task<UserProfileWithRolesDto> Handle(GetUserProfileWithRolesQuery request, CancellationToken cancellationToken)
    {
        var profile = await _db.UserProfiles
            .AsNoTracking()
            .Include(p => p.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .Where(p => p.Id == request.ProfileId && !p.IsDeleted)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new AizenBusinessException(((int)AizenErrorCode.NotFound).ToString());

        return new UserProfileWithRolesDto
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
            RoleContext = profile.RoleContext.ToString(),
            ApprovalStatus = profile.ApprovalStatus.ToString(),
            Status = profile.Status.ToString(),
            CreateDate = profile.CreateDate,
            Roles = profile.User?.UserRoles
                .Where(ur => ur.Role != null)
                .Select(ur => ur.Role!.Name ?? string.Empty)
                .Where(n => !string.IsNullOrEmpty(n))
                .ToList() ?? new List<string>()
        };
    }
}
