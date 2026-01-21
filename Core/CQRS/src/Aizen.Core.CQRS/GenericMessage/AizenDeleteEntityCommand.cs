using Aizen.Core.CQRS.Message;
using Aizen.Core.Domain;

namespace Aizen.Core.CQRS.GenericMessage;

public class AizenDeleteEntityCommand<TEntity> : AizenCommand<TEntity>
    where TEntity : AizenEntity
{
    public TEntity Entity { get; set; }
}