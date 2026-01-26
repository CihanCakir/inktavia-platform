using Aizen.Core.CQRS.Abstraction.Message;
using MiniUow.Paging;

namespace Aizen.Core.CQRS.Message;

public abstract class AizenQuery<TResult> : IAizenQuery<TResult>
{
    public string QueryId { get; }

    protected AizenQuery()
    {
        this.QueryId = Guid.NewGuid().ToString();
    }
}

public abstract class AizenListedQuery<TItem> : AizenQuery<IList<TItem>>
{
}

public abstract class AizenPagedQuery<TItem> : AizenQuery<IPaginate<TItem>>
{
}

