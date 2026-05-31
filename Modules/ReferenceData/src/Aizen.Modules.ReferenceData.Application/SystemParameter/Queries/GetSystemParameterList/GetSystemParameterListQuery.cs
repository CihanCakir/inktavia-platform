using Aizen.Core.CQRS.Message;
using Aizen.Modules.ReferenceData.Abstraction.Dto.SystemParameter;

namespace Aizen.Modules.ReferenceData.Application.SystemParameter.Queries;

public sealed class GetSystemParameterListQuery : AizenQuery<IReadOnlyList<SystemParameterDto>>
{
    public bool OnlyActive { get; }

    public GetSystemParameterListQuery(bool onlyActive = true)
    {
        OnlyActive = onlyActive;
    }
}
