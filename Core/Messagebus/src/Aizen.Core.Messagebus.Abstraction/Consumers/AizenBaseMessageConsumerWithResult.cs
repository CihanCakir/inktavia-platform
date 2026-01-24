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
                    var commitResponse = await _requestClientForCommitMessage.GetResponse<TResult>(
                        new AizenCommitMessage<TMessage>
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
                var rollbackResponse = await _requestClientForRollbackMessage.GetResponse<TResult>(
                    new AizenRollbackMessage<TMessage>
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
                var rollbackResponse = await _requestClientForRollbackMessage.GetResponse<TResult>(
                    new AizenRollbackMessage<TMessage>
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