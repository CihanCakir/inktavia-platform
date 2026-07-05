using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Application.Commands.PrepareCargoDryKitRenewal;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryRenewalPreparationsPaged;

[DocumentationInfo("GetCargoDryRenewalPreparationsPagedQueryHandler",
    "Returns paged renewal preparation list with product name enrichment. " +
    "Phase 11 (July 2026).")]
public sealed class GetCargoDryRenewalPreparationsPagedQueryHandler
    : AizenQueryHandler<GetCargoDryRenewalPreparationsPagedQuery, GetCargoDryRenewalPreparationsPagedResponse>
{
    private readonly ICargoDryRenewalPreparationRepository _preparations;
    private readonly ICargoDryProductRepository            _products;

    public GetCargoDryRenewalPreparationsPagedQueryHandler(
        ICargoDryRenewalPreparationRepository preparations,
        ICargoDryProductRepository            products)
    {
        _preparations = preparations;
        _products     = products;
    }

    public override async Task<GetCargoDryRenewalPreparationsPagedResponse> Handle(
        GetCargoDryRenewalPreparationsPagedQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;

        var (items, total) = await _preparations.GetPagedAsync(
            kitId:              request.KitId,
            kitCode:            request.KitCode,
            productCode:        request.ProductCode,
            ownerUserId:        request.OwnerUserId,
            vesselId:           request.VesselId,
            status:             request.Status,
            notificationStatus: request.NotificationStatus,
            preparedFrom:       request.PreparedFrom,
            preparedTo:         request.PreparedTo,
            skip:               skip,
            take:               request.PageSize,
            ct:                 ct);

        // Batch-load product names
        var productCodes   = items.Select(i => i.ProductCode).Distinct();
        var productsByCode = new Dictionary<string, string?>();
        foreach (var code in productCodes)
        {
            var p = await _products.GetByCodeAsync(code, ct);
            productsByCode[code] = p?.Name;
        }

        var dtos = items
            .Select(e => PrepareCargoDryKitRenewalCommandHandler.MapToDto(
                e, productsByCode.GetValueOrDefault(e.ProductCode)))
            .ToList();

        return new GetCargoDryRenewalPreparationsPagedResponse
        {
            Items    = dtos,
            Total    = total,
            Page     = request.Page,
            PageSize = request.PageSize,
            Pages    = request.PageSize > 0 ? (int)Math.Ceiling((double)total / request.PageSize) : 0,
        };
    }
}
