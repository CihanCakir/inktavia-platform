using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.GenericMessage;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.CQRS.Extention;
using Aizen.Core.UnitOfWork.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Core.CQRS.GenericHandler;

public class
    AizenInsertEntityCommandHandler<TEntity> : AizenCommandHandler<AizenInsertEntityCommand<TEntity>,
        TEntity>,IAizenGenericHandler
    where TEntity : AizenEntity
{
    public IAizenUnitOfWork UnitOfWork { get; }

    public AizenInsertEntityCommandHandler(IEnumerable<IAizenUnitOfWork> unitOfWorks)
    {
        UnitOfWork = unitOfWorks.FindUnitOfWorkForEntity<TEntity>();
    }

    public override async Task<TEntity?> Handle(AizenInsertEntityCommand<TEntity> request,
        CancellationToken cancellationToken)
    {
        var entity = request.Entity;
        var entityRepository = UnitOfWork.GetRepository<TEntity>();
        await entityRepository.AddAsync(entity);

        return entity;
    }
}