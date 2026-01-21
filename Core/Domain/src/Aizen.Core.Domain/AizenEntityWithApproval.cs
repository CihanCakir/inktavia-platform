using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Aizen.Core.Domain.Abstraction;
using Aizen.Core.Domain.Abstraction.Exception;

namespace Aizen.Core.Domain;

public interface IAizenEntity
{

}

public abstract class AizenEntityWithApproval : AizenEntityWithAudit
{
    protected AizenEntityWithApproval()
    {
        ApprovalStatus = ApprovalStatus.Pending;
        StartDate = DateTime.Now;
    }

    public bool IsActive
    {
        get
        {
            if (ApprovalStatus == ApprovalStatus.Approved)
            {
                if (EndDate.HasValue)
                {
                    return StartDate <= DateTime.Now && EndDate.Value > DateTime.Now;
                }

                return StartDate <= DateTime.Now;
            }

            return false;
        }
    }

    public ApprovalStatus ApprovalStatus { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime? EndDate { get; set; }
}

public enum ApprovalStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public enum ProfileStatus
{
    Suspended = 0,
    Inactive = 1,
    Active = 2
}

public abstract class AizenEntityWithAudit : AizenEntity
{
    public string? ModifyHost { get; set; }

    public long? ModifyUserId { get; set; }

    public DateTime? ModifyDate { get; set; }

    public long? CreateUserId { get; set; }

    public string? CreateHost { get; set; }

    public DateTime? CreateDate { get; set; }


    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public long? DeletedBy { get; set; }
    public bool IsActive { get; set; } = true;
}

public abstract class AizenEntity : IAizenEntity
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    // PostgreSQL’de uuid_generate_v4() kullanılmalı
    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public Guid? PublicId { get; set; }
    public bool IsDeleted { get; set; }

    private List<IAizenDomainEvent> _domainEvents;

    /// <summary>
    /// Add domain event.
    /// </summary>
    /// <param name="domainEvent"></param>
    protected void AddDomainEvent(IAizenDomainEvent domainEvent)
    {
        _domainEvents = _domainEvents ?? new List<IAizenDomainEvent>();
        this._domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clear domain events.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents?.Clear();
    }

    protected static void CheckRule(IAizenBusinessRule rule)
    {
        if (rule.IsBroken())
        {
            throw new AizenBusinessRuleValidationException(rule);
        }
    }
}