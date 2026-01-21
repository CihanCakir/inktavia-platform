namespace Aizen.Core.Domain.Abstraction;

public interface IAizenBusinessRule
{
    bool IsBroken();

    string Message { get; }
}