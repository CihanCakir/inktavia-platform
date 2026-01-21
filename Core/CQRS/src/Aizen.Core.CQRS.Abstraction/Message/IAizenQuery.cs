namespace Aizen.Core.CQRS.Abstraction.Message;

public interface IAizenQuery<TResult> : IAizenRequest<TResult>
{
    string QueryId { get; }
}