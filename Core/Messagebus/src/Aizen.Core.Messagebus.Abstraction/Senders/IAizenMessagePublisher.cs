using Aizen.Core.Messagebus.Abstraction.Messages;

namespace Aizen.Core.Messagebus.Abstraction.Senders;

public interface IAizenMessagePublisher
{
    Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : AizenBaseMessage;

    Task PublishRollbackAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : AizenBaseMessage;

    Task<TResponse> SendAsync<TMessage, TResponse>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : AizenBaseMessage
        where TResponse : class;
}