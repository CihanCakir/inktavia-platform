using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Queries;

public sealed class GetSystemParameterByKeyQuery : AizenQuery<SystemParameterDto?>
{
    public string Key { get; }

    public GetSystemParameterByKeyQuery(string key)
    {
        Key = key;
    }
}
