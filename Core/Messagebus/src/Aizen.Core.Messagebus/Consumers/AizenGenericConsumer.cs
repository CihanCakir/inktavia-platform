using Aizen.Core.CQRS.Abstraction;
using Aizen.Core.CQRS.GenericMessage;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Domain;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Aizen.Core.Messagebus.Consumers;

[JsonConverter(typeof(StringEnumConverter))]
public enum AizenGenericMessageOperation
{
    Create,
    Update,
    Delete,
}

public class AizenGenericMessage<TEntity> : AizenBaseMessage
{
    public AizenGenericMessageOperation Operation { get; set; }

    public TEntity Entity { get; set; }
}

public class AizenGenericMessageResult<TEntity> : AizenMessageResult
{
    public TEntity Entity { get; set; }
}

public class AizenGenericConsumer<TEntity> : AizenBaseMessageConsumer<AizenGenericMessage<TEntity>,
    AizenGenericMessageResult<TEntity>>
    where TEntity : AizenEntity
{
    private readonly IAizenCQRSProcessor _cqrsProcessor;

    public AizenGenericConsumer(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _cqrsProcessor = ServiceProvider.GetRequiredService<IAizenCQRSProcessor>();
    }

    private AizenCommand<TEntity> CreateCommand(AizenGenericMessage<TEntity> message)
    {
        switch (message.Operation)
        {
            case AizenGenericMessageOperation.Create:
                return new AizenInsertEntityCommand<TEntity>
                {
                    Entity = message.Entity
                };
            case AizenGenericMessageOperation.Update:
                return new AizenUpdateEntityCommand<TEntity>
                {
                    Entity = message.Entity
                };
            case AizenGenericMessageOperation.Delete:
                return new AizenDeleteEntityCommand<TEntity>
                {
                    Entity = message.Entity
                };
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override async Task<bool> ExecutePrepareMessage(AizenGenericMessage<TEntity> message,
        CancellationToken cancellationToken)
    {
        // HARDENING_MESSAGEBUS_GENERIC_CONSUMER_OPTOUT (defense-in-depth): a [NoMessagebusSync]
        // entity should never have a consumer registered at all (BuilderExtensions filters it
        // out). If one is somehow wired, refuse the message loudly on arrival so the generic
        // path can never bypass the domain factory/invariants for a domain-authored entity.
        if (MessagebusSyncPolicy.IsGenericSyncBlocked(typeof(TEntity)))
        {
            throw new InvalidOperationException(
                $"Entity '{typeof(TEntity).FullName}' is marked [NoMessagebusSync]; the generic " +
                "messagebus sync path is refused for it.");
        }

        if (message.Operation != AizenGenericMessageOperation.Create)
        {
            var queryById = new AizenGetEntityByIdQuery<TEntity>
            {
                Id = message.Entity.Id
            };
            var result = await _cqrsProcessor.ProcessAsync(queryById, cancellationToken);

            return result != null;
        }

        return true;
    }

    public override async Task<AizenGenericMessageResult<TEntity>> ExecuteCommitMessage(
        AizenGenericMessage<TEntity> message, CancellationToken cancellationToken)
    {
        var command = CreateCommand(message);
        var result = await _cqrsProcessor.ProcessAsync(command, cancellationToken);

        return new AizenGenericMessageResult<TEntity>
        {
            Entity = result
        };
    }

    public override Task ExecuteRollbackMessage(AizenGenericMessage<TEntity> message, AizenMessageError ex,
        CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}