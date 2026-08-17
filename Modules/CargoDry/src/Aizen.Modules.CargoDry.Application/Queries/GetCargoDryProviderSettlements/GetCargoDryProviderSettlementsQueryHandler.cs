using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetCargoDryProviderSettlements;

public sealed class GetCargoDryProviderSettlementsQueryHandler
    : AizenQueryHandler<GetCargoDryProviderSettlementsQuery, CargoDryProviderSettlementPagedResultDto>
{
    private readonly ICargoDrySellThroughSettlementRepository _settlements;

    public GetCargoDryProviderSettlementsQueryHandler(ICargoDrySellThroughSettlementRepository settlements)
    {
        _settlements = settlements;
    }

    public override async Task<CargoDryProviderSettlementPagedResultDto?> Handle(
        GetCargoDryProviderSettlementsQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;
        var status = request.Status.HasValue
            ? (CargoDrySellThroughSettlementStatus?)request.Status.Value
            : null;

        var (items, total) = await _settlements.GetPagedAsync(
            providerProfileId: request.ProviderProfileId,
            consignmentAgreementId: null,
            productCode: null,
            status: status,
            periodFrom: request.From,
            periodTo: request.To,
            search: null,
            skip: skip,
            take: request.PageSize,
            ct: ct);

        return new CargoDryProviderSettlementPagedResultDto
        {
            Items = items.Select(x => new CargoDryProviderSettlementDto
            {
                SettlementCode        = x.SettlementCode,
                ProductCode           = x.ProductCode,
                PeriodStartUtc        = x.PeriodStartUtc,
                PeriodEndUtc          = x.PeriodEndUtc,
                TotalCommissionAmount = x.TotalCommissionAmount,
                ProviderPayoutAmount  = x.ProviderPayoutAmount,
                CurrencyCode          = x.CurrencyCode,
                Status                = (int)x.Status,
                ScheduledSettlementDate = x.ScheduledSettlementDate,
                SettledAtUtc            = x.SettledAtUtc,
                PayoutCompletedAtUtc    = x.PayoutCompletedAtUtc,
            }).ToList(),
            Total    = total,
            Page     = request.Page,
            PageSize = request.PageSize,
        };
    }
}
