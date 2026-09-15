using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Application.Common;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetAdminStockRequestDetail;

/// <summary>Admin — single stock request by id.</summary>
public sealed class GetAdminStockRequestDetailQuery : AizenQuery<CargoDryStockRequestDto>
{
    public long RequestId { get; init; }
}

public sealed class GetAdminStockRequestDetailQueryHandler
    : AizenQueryHandler<GetAdminStockRequestDetailQuery, CargoDryStockRequestDto>
{
    private readonly ICargoDryStockRequestRepository _requests;

    public GetAdminStockRequestDetailQueryHandler(ICargoDryStockRequestRepository requests) => _requests = requests;

    public override async Task<CargoDryStockRequestDto?> Handle(
        GetAdminStockRequestDetailQuery request, CancellationToken ct)
    {
        var entity = await _requests.GetByIdAsync(request.RequestId, ct)
            ?? throw new AizenBusinessException($"Stock request {request.RequestId} not found.");
        return entity.ToDto();
    }
}
