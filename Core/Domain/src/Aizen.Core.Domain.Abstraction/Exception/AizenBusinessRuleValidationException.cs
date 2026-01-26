using Aizen.Core.Common.Abstraction.Exception;

namespace Aizen.Core.Domain.Abstraction.Exception;

public class AizenBusinessRuleValidationException : AizenException
{
    public IAizenBusinessRule BrokenRule { get; }

    public string Details { get; }

    public AizenBusinessRuleValidationException(IAizenBusinessRule brokenRule) : base(brokenRule.Message)
    {
        BrokenRule = brokenRule;
        this.Details = brokenRule.Message;
    }

    public override string ToString()
    {
        return $"{BrokenRule.GetType().FullName}: {BrokenRule.Message}";
    }
}