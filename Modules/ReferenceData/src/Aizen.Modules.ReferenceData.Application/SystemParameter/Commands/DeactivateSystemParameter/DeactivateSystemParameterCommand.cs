using Aizen.Core.CQRS.Message;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Commands;

public sealed class DeactivateSystemParameterCommand : AizenCommand<bool>
{
    public string Key { get; }

    public DeactivateSystemParameterCommand(string key)
    {
        Key = key;
    }
}
