using Aizen.Core.Domain.Abstraction;

namespace Aizen.Core.Domain;

public abstract class AizenDomainEventBase: IAizenDomainEvent
{
    public DateTime OccurredOn { get; }
    
    public AizenDomainEventBase()
    {
        this.OccurredOn = DateTime.Now;
    }
}