using Aizen.Core.CQRS.Handler;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Marina;
using Aizen.Modules.ReferenceData.Domain.Interface.Service;

namespace Aizen.Modules.ReferenceData.Application.Marina.Queries;

[DocumentationInfo("Returns a single marina by id", "Used by callers that need to resolve a marina reference to its name and coordinates.")]
public sealed class GetMarinaByIdQueryHandler : AizenQueryHandler<GetMarinaByIdQuery, MarinaDto?>
{
    private readonly IMarinaReferenceService _service;

    public GetMarinaByIdQueryHandler(IMarinaReferenceService service)
    {
        _service = service;
    }

    public override async Task<MarinaDto?> Handle(GetMarinaByIdQuery request, CancellationToken cancellationToken)
    {
        return await _service.GetByIdAsync(request.Id, cancellationToken);
    }
}
