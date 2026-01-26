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
                    await context.Publish(new AizenCommitMessage<TMessage>
                        {Id = payload.Id, Message = payload.Message});
                }
            }
            catch (Exception ex)
            {
                Elastic.Apm.Agent.Tracer.CurrentTransaction?.CaptureException(ex);
                await context.Publish(new AizenRollbackMessage<TMessage>
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
                await context.Publish(new AizenRollbackMessage<TMessage>
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