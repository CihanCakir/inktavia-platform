using MassTransit;
using Aizen.Core.Common.Abstraction.Exception;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.Messagebus;

public class AizenMessagePublisher : IAizenMessagePublisher
{
    private readonly IServiceProvider _serviceProvider;

    public AizenMessagePublisher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task PublishAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : AizenBaseMessage
    {
        var preparedMessage = new AizenPrepareMessage<TMessage>
        {
            Id = Guid.NewGuid().ToString(),
            Message = message
        };
        var publishEndpoint = _serviceProvider.GetRequiredService<IPublishEndpoint>();
        await publishEndpoint.Publish(preparedMessage, cancellationToken);
    }

    public async Task PublishRollbackAsync<TMessage>(TMessage message, CancellationToken cancellationToken = default)
        where TMessage : AizenBaseMessage
    {
        var preparedMessage = new AizenRollbackMessage<TMessage>
        {
            Id = Guid.NewGuid().ToString(),
            Message = message
        };
        var publishEndpoint = _serviceProvider.GetRequiredService<IPublishEndpoint>();
        await publishEndpoint.Publish(preparedMessage, cancellationToken);
    }

    public async Task<TResponse> SendAsync<TMessage, TResponse>(TMessage message, CancellationToken cancellationToken)
        where TMessage : AizenBaseMessage
        where TResponse : class
    {
        var preparedMessage = new AizenPrepareMessage<TMessage>
        {
            Id = Guid.NewGuid().ToString(),
            Message = message
        };
        var requestClient = _serviceProvider.GetRequiredService<IRequestClient<AizenPrepareMessage<TMessage>>>();
        var response = await requestClient.GetResponse<TResponse>(preparedMessage, cancellationToken);
        return response.Message;
    }

}