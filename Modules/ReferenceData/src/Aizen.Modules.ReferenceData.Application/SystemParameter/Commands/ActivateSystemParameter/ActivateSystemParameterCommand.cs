using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Commands;

public sealed class ActivateSystemParameterCommand : AizenCommand<bool>
{
    public string Key { get; }

    public ActivateSystemParameterCommand(string key)
    {
        Key = key;
    }
}
