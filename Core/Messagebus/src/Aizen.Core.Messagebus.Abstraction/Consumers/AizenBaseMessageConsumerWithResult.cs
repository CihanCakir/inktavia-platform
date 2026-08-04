using MassTransit;
using Aizen.Core.Common.Abstraction.Exception;
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Microsoft.Extensions.DependencyInjection;

namespace Aizen.Core.Messagebus.Abstraction.Consumers
{
    public abstract class AizenBaseMessageConsumer<TMessage, TResult> : IAizenMessageConsumer<TMessage>
        where TMessage : AizenBaseMessage
        where TResult : AizenMessageResult, new()
    {
        protected readonly IServiceProvider ServiceProvider;
        private readonly IRequestClient<AizenPrepareMessage<TMessage>> _requestClientForPrepareMessage;
        private readonly IRequestClient<AizenCommitMessage<TMessage>> _requestClientForCommitMessage;
        private readonly IRequestClient<AizenRollbackMessage<TMessage>> _requestClientForRollbackMessage;

        protected AizenBaseMessageConsumer(IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
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
                    // WS2 exactly-once: request the Commit DIRECTED to this consumer's own receive queue
                    // instead of via the DI request client, which publishes to the shared
                    // {TMessage}.AizenCommitMessage exchange → fans to every K consumer of TMessage → P
                    // executions of ExecuteCommitMessage. A per-address request client targets only the
                    // preparing consumer's queue → P≡1, while preserving the GetResponse<TResult> correlation
                    // (request/response correlates on RequestId, independent of destination).
                    var commitResponse = await CreateOwnEndpointClient<AizenCommitMessage<TMessage>>(context)
                        .GetResponse<TResult>(new AizenCommitMessage<TMessage>
                            {Id = payload.Id, Message = payload.Message});
                    await context.RespondAsync(commitResponse.Message);
                }
                else
                {
                    throw new AizenException($"Prepare message failed. for {typeof(TMessage).Name}");
                }
            }
            catch (Exception ex)
            {
                // WS2: keep the Rollback 1:1 with the preparing consumer too — directed, not fanned.
                var rollbackResponse = await CreateOwnEndpointClient<AizenRollbackMessage<TMessage>>(context)
                    .GetResponse<TResult>(new AizenRollbackMessage<TMessage>
                    {
                        Id = payload.Id, Message = payload.Message, Exception = new AizenMessageError
                        {
                            ErrorSource = AizenMessageErrorSource.Prepare,
                            Message = ex.Message,
                            StackTrace = ex.StackTrace
                        }
                    });
                await context.RespondAsync(rollbackResponse.Message);
            }
        }

        public async Task Consume(ConsumeContext<AizenCommitMessage<TMessage>> context)
        {
            var payload = context.Message;
            try
            {
                var result = await this.ExecuteCommitMessage(payload.Message, CancellationToken.None);
                result.Id = payload.Id;
                result.IsSuccess = true;
                await context.RespondAsync(result);
            }
            catch (Exception ex)
            {
                // WS2: Commit-failure Rollback stays directed to the preparing consumer's own queue.
                var rollbackResponse = await CreateOwnEndpointClient<AizenRollbackMessage<TMessage>>(context)
                    .GetResponse<TResult>(new AizenRollbackMessage<TMessage>
                    {
                        Id = payload.Id, Message = payload.Message,
                        Exception = new AizenMessageError
                        {
                            ErrorSource = AizenMessageErrorSource.Commit,
                            Message = ex.Message,
                            StackTrace = ex.StackTrace
                        }
                    });
                await context.RespondAsync(rollbackResponse.Message);
            }
        }

        // WS2 exactly-once: build a request client bound to this consumer's OWN receive queue
        // (context.ReceiveContext.InputAddress) so the Commit/Rollback request is delivered only to the
        // consumer that prepared — never fanned to the other K-1 consumers of TMessage. The queue is shared by
        // a service's replicas, so the directed request is still handled by exactly one replica (competing
        // consumers) → exactly-once with N replicas. GetResponse<TResult> correlation is unaffected (RequestId).
        private IRequestClient<T> CreateOwnEndpointClient<T>(ConsumeContext context)
            where T : class
            => ServiceProvider.GetRequiredService<IBus>()
                .CreateRequestClient<T>(context.ReceiveContext.InputAddress);

        public async Task Consume(ConsumeContext<AizenRollbackMessage<TMessage>> context)
        {
            var payload = context.Message;
            try
            {
                await this.ExecuteRollbackMessage(payload.Message, payload.Exception, CancellationToken.None);
                await context.RespondAsync(new TResult
                {
                    Id = payload.Id,
                    IsSuccess = false,
                    Exception = new AizenMessageError
                    {
                        IsRollbacked = true,
                        Message = payload.Exception.Message,
                        StackTrace = payload.Exception.StackTrace
                    }
                });
            }
            catch (Exception ex)
            {
                await context.RespondAsync(new TResult
                {
                    Id = payload.Id,
                    IsSuccess = false,
                    Exception = new AizenMessageError
                    {
                        ErrorSource = AizenMessageErrorSource.Rollback,
                        IsRollbacked = false,
                        Message = ex.Message,
                        StackTrace = ex.StackTrace
                    }
                });
                throw;
            }
        }

        public abstract Task<bool> ExecutePrepareMessage(TMessage message, CancellationToken cancellationToken);

        public abstract Task<TResult> ExecuteCommitMessage(TMessage message, CancellationToken cancellationToken);

        public abstract Task ExecuteRollbackMessage(TMessage message, AizenMessageError ex, CancellationToken cancellationToken);
    }
}