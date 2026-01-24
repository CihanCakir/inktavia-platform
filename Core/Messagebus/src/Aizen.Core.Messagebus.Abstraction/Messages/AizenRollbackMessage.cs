namespace Aizen.Core.Messagebus.Abstraction.Messages
{
    public sealed class AizenRollbackMessage<TMessage> where TMessage : AizenBaseMessage
    {
        public string Id { get; set; }
        
        public TMessage Message { get; set; }

        public AizenMessageError Exception { get; set; }

    }
}
