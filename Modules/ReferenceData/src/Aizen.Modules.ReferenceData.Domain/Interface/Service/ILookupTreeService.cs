using Aizen.Modules.ReferenceData.Abstraction.Dto.LookupGroup;
using Aizen.Modules.ReferenceData.Abstraction.Request.LookupGroup;

namespace Aizen.Modules.ReferenceData.Domain.Interface.Service;

public interface ILookupTreeService
{
    Task<IReadOnlyList<LookupGroupTreeDto>> GetTreeAsync(bool onlyActive, CancellationToken cancellationToken = default);
    Task<LookupGroupTreeDto> MoveGroupAsync(MoveLookupGroupRequest request, CancellationToken cancellationToken = default);
}
