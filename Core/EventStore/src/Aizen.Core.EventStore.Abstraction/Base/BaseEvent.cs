using EventStore.Client;

namespace Aizen.Core.EventStore.Abstraction;

public abstract class BaseEvent<T>
    where T : BaseEvent<T>
{
    public Uuid EventId { get; set; } = Uuid.NewUuid();
    public string EventType { get; set; } = typeof(T).Name;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan TimeToLive { get; set; } = TimeSpan.FromDays(30);

    public abstract string UserId { get; set; }
    public abstract string Phone { get; set; }
    public abstract decimal Amount { get; set; }
    public abstract TransactionType TransactionType { get; set; }
}

public enum SourceType
{
    CreditCard,
    AizenCard
}

public enum TargetType
{
    AizenCard,
    AizenMerchant,
    Product
}

public enum TransactionType
{
    Deposit,
    Transfer,
    Spend,
    SpendBatch,
    Buy,
    CardOperation
}