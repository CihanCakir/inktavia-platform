namespace Aizen.Core.Messagebus.Abstraction.Messages
{
    public enum AizenMessageErrorSource
    {
        Prepare,
        Commit,
        Rollback
    }
    
    public abstract class AizenBaseMessage : IAizenMessage
    {
    }

    public class AizenMessageResult
    {
        public string Id { get; set; }
        
        public bool IsSuccess { get; set; }

        public AizenMessageError Exception { get; set; }
    }

    public class AizenMessageError
    {
        public string Message { get; set; }

        public AizenMessageErrorSource ErrorSource { get; set; }
        
        public string? StackTrace { get; set; }

        public bool IsRollbacked { get; set; }
    }
}