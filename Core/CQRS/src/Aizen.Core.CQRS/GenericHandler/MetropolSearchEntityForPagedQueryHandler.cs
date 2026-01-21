using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.GenericMessage;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Library;
using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.CQRS.Extention;
using Aizen.Core.UnitOfWork.Abstraction;
using Microsoft.EntityFrameworkCore;
using MiniUow.Paging;

namespace Aizen.Core.CQRS.GenericHandler;

public class
    AizenSearchEntityForPagedQueryHandler<TEntity> : AizenQueryHandler<
        AizenSearchEntityForPagedQuery<TEntity>, IPaginate<TEntity>>, IAizenGenericHandler
    where TEntity : AizenEntity
{
    public IAizenUnitOfWork UnitOfWork { get; }

    public AizenSearchEntityForPagedQueryHandler(IEnumerable<IAizenUnitOfWork> unitOfWorks)
    {
        UnitOfWork = unitOfWorks.FindUnitOfWorkForEntity<TEntity>();
    }

    public override async Task<IPaginate<TEntity>> Handle(AizenSearchEntityForPagedQuery<TEntity> request,
        CancellationToken cancellationToken)
    {
        var entityRepository = UnitOfWork.GetRepository<TEntity>();
        var predicate = QueryService<TEntity>.ApplyFilters(request.Search, request.Filters);
        var entities = await entityRepository.GetPagedListAsync(
            predicate,
            index: request.Index,
            size: request.Size,
            cancellationToken: cancellationToken);

        return entities;
    }
}