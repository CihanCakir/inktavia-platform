using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Queries;

public sealed class GetSystemParametersByPrefixQuery : AizenQuery<IReadOnlyList<SystemParameterDto>>
{
    public string Prefix { get; }
    public bool OnlyActive { get; }

    public GetSystemParametersByPrefixQuery(string prefix, bool onlyActive = true)
    {
        Prefix = prefix;
        OnlyActive = onlyActive;
    }
}
