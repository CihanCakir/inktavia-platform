using Aizen.Core.CQRS.Abstraction.Message;

namespace Aizen.Core.CQRS.Message;

public abstract class AizenCommand : IAizenCommand
{
    public string CommandId { get; set; }

    protected AizenCommand()
    {
        this.CommandId = Guid.NewGuid().ToString();
    }
}

public abstract class AizenCommand<TResult> : IAizenCommand<TResult>
{
    public string CommandId { get; }

    protected AizenCommand()
    {
        this.CommandId = Guid.NewGuid().ToString();
    }
}