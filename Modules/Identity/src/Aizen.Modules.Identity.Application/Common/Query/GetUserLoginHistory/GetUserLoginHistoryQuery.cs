using Aizen.Core.CQRS.Message;
using Aizen.Modules.Identity.Abstraction.Dto.Common;

namespace Aizen.Modules.InktaviaStore.Application.Identity.Query.Common;

public sealed class GetUserLoginHistoryQuery : AizenQuery<List<UserLoginHistoryItemDto>>
{
    public long UserId { get; }
    public int PageSize { get; }

    public GetUserLoginHistoryQuery(long userId, int pageSize = 50)
    {
        UserId = userId;
        PageSize = Math.Clamp(pageSize, 1, 200);
    }
}
