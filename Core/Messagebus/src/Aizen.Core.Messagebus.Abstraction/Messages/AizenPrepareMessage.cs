namespace Aizen.Core.Messagebus.Abstraction.Messages
{
    public sealed class AizenPrepareMessage<TMessage> where TMessage : AizenBaseMessage
    {
        public string Id { get; set; }
        
        public TMessage Message { get; set; }

    }
}
