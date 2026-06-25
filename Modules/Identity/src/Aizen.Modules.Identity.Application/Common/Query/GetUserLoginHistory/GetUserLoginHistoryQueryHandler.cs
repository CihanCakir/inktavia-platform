using Aizen.Core.CQRS.Handler;
using Aizen.Modules.Identity.Abstraction.Dto.Common;
using Aizen.Modules.Identity.Repository.Context;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Common;

public sealed class GetUserLoginHistoryQueryHandler
    : AizenQueryHandler<GetUserLoginHistoryQuery, List<UserLoginHistoryItemDto>>
{
    private readonly IdentityDbContext _db;

    public GetUserLoginHistoryQueryHandler(IdentityDbContext db)
    {
        _db = db;
    }

    public override async Task<List<UserLoginHistoryItemDto>?> Handle(
        GetUserLoginHistoryQuery request, CancellationToken cancellationToken)
    {
        return await _db.UserLoginTokens
            .AsNoTracking()
            .Where(t => t.UserId == request.UserId)
            .OrderByDescending(t => t.CreateDate)
            .Take(request.PageSize)
            .Select(t => new UserLoginHistoryItemDto
            {
                Id = t.Id,
                RoleContext = t.RoleContext.ToString(),
                IsRevoked = t.IsRevoked,
                LoginAt = t.CreateDate
            })
            .ToListAsync(cancellationToken);
    }
}
