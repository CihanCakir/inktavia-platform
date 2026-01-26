using Microsoft.Extensions.DependencyInjection;
using Aizen.Core.Messagebus.Abstraction.Consumers;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Realtime.Abstraction.Interfaces;
using Aizen.Core.Realtime.Abstraction.Models;

namespace Aizen.Core.Realtime.MessageConsumers
{
    public class RealtimeEventConsumer<TMessage, TResult> : AizenBaseMessageConsumer<TMessage, TResult>
        where TMessage : AizenBaseMessage
        where TResult : AizenMessageResult, new()
    {
        private readonly IRealtimeEventIngress _ingress;

        public RealtimeEventConsumer(IServiceProvider sp) : base(sp)
        {
            _ingress = sp.GetRequiredService<IRealtimeEventIngress>();
        }

        public override Task<bool> ExecutePrepareMessage(TMessage message, CancellationToken cancellationToken)
            => Task.FromResult(true);

        public override async Task<TResult> ExecuteCommitMessage(TMessage message, CancellationToken cancellationToken)
        {
            var evt = new EventDto
            {
                Id = System.Guid.NewGuid().ToString(),
                AggregateId = "", // map from TMessage in concrete consumer
                Type = typeof(TMessage).Name,
                Data = message,
                CreatedAt = System.DateTimeOffset.UtcNow
            };

            await _ingress.PublishDomainEventAsync(evt, cancellationToken);

            var result = new TResult { Id = evt.Id, IsSuccess = true };
            return result;
        }

        public override Task ExecuteRollbackMessage(TMessage message, Aizen.Core.Messagebus.Abstraction.Messages.AizenMessageError ex, CancellationToken cancellationToken)
            => Task.CompletedTask;
    }
}