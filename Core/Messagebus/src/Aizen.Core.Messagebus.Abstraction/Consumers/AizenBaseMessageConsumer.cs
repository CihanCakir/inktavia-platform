using MassTransit;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.Messagebus.Abstraction.Consumers
{
    public abstract class AizenBaseMessageConsumer<TMessage> : IAizenMessageConsumer<TMessage>
        where TMessage : AizenBaseMessage
    {
        private readonly IRequestClient<AizenPrepareMessage<TMessage>> _requestClientForPrepareMessage;
        private readonly IRequestClient<AizenCommitMessage<TMessage>> _requestClientForCommitMessage;
        private readonly IRequestClient<AizenRollbackMessage<TMessage>> _requestClientForRollbackMessage;


        protected AizenBaseMessageConsumer(IServiceProvider serviceProvider)
        {
            _requestClientForPrepareMessage =
                serviceProvider.GetRequiredService<IRequestClient<AizenPrepareMessage<TMessage>>>();
            _requestClientForCommitMessage =
                serviceProvider.GetRequiredService<IRequestClient<AizenCommitMessage<TMessage>>>();
            _requestClientForRollbackMessage =
                serviceProvider.GetRequiredService<IRequestClient<AizenRollbackMessage<TMessage>>>();
        }

        public async Task Consume(ConsumeContext<AizenPrepareMessage<TMessage>> context)
        {
            var payload = context.Message;
            try
            {
                var result = await this.ExecutePrepareMessage(payload.Message, CancellationToken.None);
                if (result)
                {
                    // WS2 exactly-once: Commit must reach ONLY the consumer that prepared. Publishing the
                    // commit fans it to the shared {TMessage}.AizenCommitMessage exchange → every K consumer of
                    // TMessage runs ExecuteCommitMessage (P executions). Send it DIRECTED to this consumer's own
                    // receive queue instead → P≡1 by construction. Prepare fan-out is intentionally left as-is.
                    await SendToOwnEndpoint(context, new AizenCommitMessage<TMessage>
                        {Id = payload.Id, Message = payload.Message});
                }
            }
            catch (Exception ex)
            {
                Elastic.Apm.Agent.Tracer.CurrentTransaction?.CaptureException(ex);
                await SendToOwnEndpoint(context, new AizenRollbackMessage<TMessage>
                {
                    Id = payload.Id, Message = payload.Message, Exception = new AizenMessageError
                    {
                        ErrorSource = AizenMessageErrorSource.Prepare,
                        Message = ex.Message,
                        StackTrace = ex.StackTrace
                    }
                });
            }
        }

        public async Task Consume(ConsumeContext<AizenCommitMessage<TMessage>> context)
        {
            var payload = context.Message;
            try
            {
                await this.ExecuteCommitMessage(payload.Message, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Elastic.Apm.Agent.Tracer.CurrentTransaction?.CaptureException(ex);
                // WS2: keep the Rollback 1:1 with the preparing consumer too — directed, not fanned.
                await SendToOwnEndpoint(context, new AizenRollbackMessage<TMessage>
                {
                    Id = payload.Id, Message = payload.Message, Exception = new AizenMessageError
                    {
                        ErrorSource = AizenMessageErrorSource.Commit,
                        Message = ex.Message,
                        StackTrace = ex.StackTrace
                    }
                });
            }
        }

        // WS2 exactly-once: dispatch a wrapper message DIRECTED to this consumer's own receive queue
        // (context.ReceiveContext.InputAddress) instead of publishing it to the shared, consumer-agnostic
        // exchange. Only the consumer that prepared receives its Commit/Rollback → no cross-consumer fan-out.
        // The queue is shared by a service's replicas, so a directed send is still picked up by exactly one
        // replica (competing consumers) → exactly-once with N replicas.
        private static async Task SendToOwnEndpoint<T>(ConsumeContext context, T message)
            where T : class
        {
            var endpoint = await context.GetSendEndpoint(context.ReceiveContext.InputAddress);
            await endpoint.Send(message);
        }

        public async Task Consume(ConsumeContext<AizenRollbackMessage<TMessage>> context)
        {
            var payload = context.Message;
            try
            {
                await this.ExecuteRollbackMessage(payload.Message, payload.Exception, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Elastic.Apm.Agent.Tracer.CurrentTransaction?.CaptureException(ex);
                throw;
            }
        }

        public abstract Task<bool> ExecutePrepareMessage(TMessage message, CancellationToken cancellationToken);

        public abstract Task ExecuteCommitMessage(TMessage message, CancellationToken cancellationToken);

        public abstract Task ExecuteRollbackMessage(TMessage message, AizenMessageError ex,
            CancellationToken cancellationToken);
    }
}