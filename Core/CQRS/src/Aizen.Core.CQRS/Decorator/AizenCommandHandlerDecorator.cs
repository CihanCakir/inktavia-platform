using Aizen.Core.Common.Abstraction.Exception;
using Aizen.Core.CQRS.Abstraction.Handler;
using Aizen.Core.CQRS.Abstraction.Message;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.UnitOfWork.Abstraction;

namespace Aizen.Core.CQRS.Decorator;

internal sealed class AizenCommandHandlerDecorator<TCommand, TResult> : AizenCommandHandler<TCommand, TResult>,
    IAizenRequestDecorator<TCommand, TResult>
    where TCommand : IAizenCommand<TResult>
{
    public IAizenRequestHandler<TCommand, TResult> Decorated => _decorated;

    private readonly IAizenCommandHandler<TCommand, TResult> _decorated;

    IEnumerable<IAizenUnitOfWork> _unitOfWorks;

    public AizenCommandHandlerDecorator(
        IAizenCommandHandler<TCommand, TResult> decorated,
        IEnumerable<IAizenUnitOfWork> unitOfWorks)
    {
        _decorated = decorated;
        _unitOfWorks = unitOfWorks;
    }

    public override async Task<TResult?> Handle(TCommand command, CancellationToken cancellationToken)
    {
        foreach (var unitOfWork in _unitOfWorks)
        {
            if (_decorated.IsTransactional)
            {
                await unitOfWork.DbContext.Database.BeginTransactionAsync();
            }
        }

        try
        {
            var result = await _decorated.Handle(command, cancellationToken);

            foreach (var unitOfWork in _unitOfWorks)
            {
                await unitOfWork.SaveChangesAsync();
                if (_decorated.IsTransactional)
                {
                    await unitOfWork.DbContext.Database.CommitTransactionAsync(cancellationToken);
                }
            }

            return result;
        }
        catch (System.Exception ex) when (ex is AizenException { IsRollback: false })
        {
            foreach (var unitOfWork in _unitOfWorks)
            {
                if (unitOfWork.DbContext.Database.CurrentTransaction != null)
                {
                    if (_decorated.IsTransactional)
                    {
                        await unitOfWork.DbContext.Database.CommitTransactionAsync(cancellationToken);
                    }
                }
            }

            throw;
        }
        catch (Exception ex)
        {
            foreach (var unitOfWork in _unitOfWorks)
            {
                if (unitOfWork.DbContext.Database.CurrentTransaction != null)
                {
                    if (_decorated.IsTransactional)
                    {
                        await unitOfWork.DbContext.Database.RollbackTransactionAsync(cancellationToken);
                    }
                }
            }

            throw;
        }
    }
}