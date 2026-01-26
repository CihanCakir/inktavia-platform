using Aizen.Core.CQRS.Library;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Domain;

namespace Aizen.Core.CQRS.GenericMessage;

public class AizenSearchEntityForPagedQuery<TEntity> : AizenPagedQuery<TEntity>
    where TEntity : AizenEntity
{
    public TEntity Search { get; set; }

    public List<Filter> Filters { get; set; }
    public int Index { get; set; }
    public int Size { get; set; }

    public AizenSearchEntityForPagedQuery()
    {
        Filters = new List<Filter>();
    }
}