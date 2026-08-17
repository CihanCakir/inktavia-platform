using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetProviderStockRequests;

public sealed class GetProviderStockRequestsQueryHandler
    : AizenQueryHandler<GetProviderStockRequestsQuery, CargoDryStockRequestPagedResultDto>
{
    private readonly ICargoDryStockRequestRepository _requests;

    public GetProviderStockRequestsQueryHandler(ICargoDryStockRequestRepository requests)
        => _requests = requests;

    public override async Task<CargoDryStockRequestPagedResultDto> Handle(
        GetProviderStockRequestsQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var (items, total) = await _requests.GetByProviderAsync(
            request.ProviderProfileId, request.Status, skip, request.PageSize, ct);

        return new CargoDryStockRequestPagedResultDto
        {
            Items = items.Select(e => new CargoDryStockRequestDto
            {
                Id = e.Id, RequestCode = e.RequestCode,
                ProviderProfileId = e.ProviderProfileId, ProductCode = e.ProductCode,
                RequestedQuantity = e.RequestedQuantity,
                Status = (int)e.Status, StatusName = e.Status.ToString(),
                ProviderNote = e.ProviderNote, ConsignmentAgreementId = e.ConsignmentAgreementId,
                DecidedAtUtc = e.DecidedAtUtc, DecisionNote = e.DecisionNote,
                ApprovedBatchCode = e.ApprovedBatchCode, AllocatedQuantity = e.AllocatedQuantity,
                CreatedAtUtc = e.CreateDate.HasValue ? new DateTimeOffset(e.CreateDate.Value, TimeSpan.Zero) : DateTimeOffset.UtcNow
            }).ToList(),
            Total = total, Page = request.Page, PageSize = request.PageSize
        };
    }
}
