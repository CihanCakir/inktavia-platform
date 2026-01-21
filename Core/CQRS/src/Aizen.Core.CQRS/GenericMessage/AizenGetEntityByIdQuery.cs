using Aizen.Core.CQRS.Message;
using Aizen.Core.Domain;

namespace Aizen.Core.CQRS.GenericMessage;

public class AizenGetEntityByIdQuery<TEntity> : AizenQuery<TEntity>
    where TEntity : AizenEntity
{
    public long Id { get; set; }
}