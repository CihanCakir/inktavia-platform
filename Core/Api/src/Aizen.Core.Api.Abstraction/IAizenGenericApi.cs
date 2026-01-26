using Aizen.Core.CQRS.Library;
using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.Api;
using MiniUow.Paging;

namespace Aizen.Core.Api.Abstraction;

public class AizenGenericRequest<TEntity>
    where TEntity : AizenEntity
{
    public TEntity Entity { get; set; }

    public List<Filter> Filters { get; set; }
}

public interface IAizenGenericApi<TEntity>
    where TEntity : AizenEntity
{
    Task<AizenApiResponse<TEntity>> GetEntity(int id, CancellationToken cancellationToken);

    Task<AizenApiResponse<IList<TEntity>>> SearchEntityForList(AizenGenericRequest<TEntity> search,
        CancellationToken cancellationToken);

    Task<AizenApiResponse<IPaginate<TEntity>>> SearchEntityForPaged(AizenGenericRequest<TEntity> search,
        int index, int size, CancellationToken cancellationToken);
}