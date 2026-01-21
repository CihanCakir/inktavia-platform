using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.GenericMessage;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Library;
using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.CQRS.Extention;
using Aizen.Core.UnitOfWork.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Core.CQRS.GenericHandler;

public class
    AizenSearchEntityForListQueryHandler<TEntity> : AizenQueryHandler<
        AizenSearchEntityForListQuery<TEntity>, IList<TEntity>>, IAizenGenericHandler
    where TEntity : AizenEntity
{
    public IAizenUnitOfWork UnitOfWork { get; }

    public AizenSearchEntityForListQueryHandler(IEnumerable<IAizenUnitOfWork> unitOfWorks)
    {
        UnitOfWork = unitOfWorks.FindUnitOfWorkForEntity<TEntity>();
    }

    public override async Task<IList<TEntity>> Handle(AizenSearchEntityForListQuery<TEntity> request,
        CancellationToken cancellationToken)
    {
        var entityRepository = UnitOfWork.GetRepository<TEntity>();
        var predicate = QueryService<TEntity>.ApplyFilters(request.Search, request.Filters);
        var query = await entityRepository.GetAllAsync(predicate);
        var entities = query.ToList();

        return entities;
    }
}