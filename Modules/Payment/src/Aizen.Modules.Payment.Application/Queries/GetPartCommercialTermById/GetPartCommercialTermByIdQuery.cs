using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Payment.Abstraction.Enum;
using Aizen.Modules.Payment.Application.Dto;
using Aizen.Modules.Payment.Domain.Interface.Repository;

namespace Aizen.Modules.Payment.Application.Queries.GetPartCommercialTermById;

/// <summary>BE-S5a — admin detail of a single part commercial term (admin-only; carries cost).</summary>
public sealed class GetPartCommercialTermByIdQuery : AizenQuery<PartCommercialTermDto>
{
    public required long Id { get; init; }
}

[DocumentationInfo("GetPartCommercialTermByIdQueryHandler", "Returns one part commercial term for the admin detail screen.")]
public sealed class GetPartCommercialTermByIdQueryHandler
    : AizenQueryHandler<GetPartCommercialTermByIdQuery, PartCommercialTermDto>
{
    private readonly IPartCommercialTermRepository _terms;
    public GetPartCommercialTermByIdQueryHandler(IPartCommercialTermRepository terms) => _terms = terms;

    public override async Task<PartCommercialTermDto?> Handle(GetPartCommercialTermByIdQuery request, CancellationToken ct)
    {
        var term = await _terms.GetByIdAsync(request.Id, ct)
            ?? throw new AizenBusinessException(
                (int)PaymentErrorCode.PartCommercialTermNotFound, $"Part commercial term {request.Id} not found.");
        return PartCommercialTermDtoMapper.ToDto(term);
    }
}
