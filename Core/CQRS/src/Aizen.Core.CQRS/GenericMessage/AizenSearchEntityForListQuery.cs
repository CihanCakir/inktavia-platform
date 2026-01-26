using Aizen.Core.CQRS.Library;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Domain;

namespace Aizen.Core.CQRS.GenericMessage;

public class AizenSearchEntityForListQuery<TEntity> : AizenListedQuery<TEntity>
    where TEntity : AizenEntity
{
    public TEntity Search { get; set; }

    public List<Filter> Filters { get; set; }

    public AizenSearchEntityForListQuery()
    {
        Filters = new List<Filter>();
    }
}