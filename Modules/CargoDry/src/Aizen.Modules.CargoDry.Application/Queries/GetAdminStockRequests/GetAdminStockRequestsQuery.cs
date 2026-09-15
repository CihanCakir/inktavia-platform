using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Application.Common;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetAdminStockRequests;

/// <summary>Admin (cross-provider) paged stock-request list — optional status + providerProfileId filters.</summary>
public sealed class GetAdminStockRequestsQuery : AizenQuery<CargoDryStockRequestPagedResultDto>
{
    public CargoDryStockRequestStatus? Status            { get; init; }
    public long?                       ProviderProfileId { get; init; }
    public int                         Page              { get; init; } = 1;
    public int                         PageSize          { get; init; } = 25;
}

public sealed class GetAdminStockRequestsQueryHandler
    : AizenQueryHandler<GetAdminStockRequestsQuery, CargoDryStockRequestPagedResultDto>
{
    private readonly ICargoDryStockRequestRepository _requests;

    public GetAdminStockRequestsQueryHandler(ICargoDryStockRequestRepository requests) => _requests = requests;

    public override async Task<CargoDryStockRequestPagedResultDto> Handle(
        GetAdminStockRequestsQuery request, CancellationToken ct)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var (items, total) = await _requests.GetPagedAsync(
            request.Status, request.ProviderProfileId, (page - 1) * pageSize, pageSize, ct);

        return new CargoDryStockRequestPagedResultDto
        {
            Items    = items.Select(e => e.ToDto()).ToList(),
            Total    = total,
            Page     = page,
            PageSize = pageSize,
        };
    }
}
