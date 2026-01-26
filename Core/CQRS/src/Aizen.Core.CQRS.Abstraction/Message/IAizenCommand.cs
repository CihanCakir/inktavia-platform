namespace Aizen.Core.CQRS.Abstraction.Message;

public interface IAizenCommand : IAizenRequest<bool>
{
    string CommandId { get; set; }
}

public interface IAizenCommand<out TResult> : IAizenRequest<TResult>
{
    string CommandId { get; }
}