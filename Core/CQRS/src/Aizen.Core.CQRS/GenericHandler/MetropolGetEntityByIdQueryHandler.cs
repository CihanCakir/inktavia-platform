using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.GenericMessage;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Domain;
using Aizen.Core.Infrastructure.CQRS.Extention;
using Aizen.Core.UnitOfWork.Abstraction;

namespace Aizen.Core.CQRS.GenericHandler;

public class
    AizenGetEntityByIdQueryHandler<TEntity> : AizenQueryHandler<AizenGetEntityByIdQuery<TEntity>,
        TEntity>, IAizenGenericHandler
    where TEntity : AizenEntity
{
    public IAizenUnitOfWork UnitOfWork { get; }

    public AizenGetEntityByIdQueryHandler(IEnumerable<IAizenUnitOfWork> unitOfWorks)
    {
        UnitOfWork = unitOfWorks.FindUnitOfWorkForEntity<TEntity>();
    }

    public override async Task<TEntity> Handle(AizenGetEntityByIdQuery<TEntity> request,
        CancellationToken cancellationToken)
    {
        var entityRepository = UnitOfWork.GetRepository<TEntity>();
        var entity = await entityRepository.FirstOrDefaultAsync(x => x.Id == request.Id);

        return entity;
    }
}