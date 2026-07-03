using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Abstraction.Dto;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;

namespace Aizen.Modules.CargoDry.Application.Queries.GetConsignmentAgreementsPaged;

public sealed class GetConsignmentAgreementsPagedQueryHandler
    : AizenQueryHandler<GetConsignmentAgreementsPagedQuery, CargoDryConsignmentAgreementPagedResultDto>
{
    private readonly ICargoDryConsignmentAgreementRepository _agreements;

    public GetConsignmentAgreementsPagedQueryHandler(
        ICargoDryConsignmentAgreementRepository agreements)
    {
        _agreements = agreements;
    }

    public override async Task<CargoDryConsignmentAgreementPagedResultDto> Handle(
        GetConsignmentAgreementsPagedQuery request, CancellationToken ct)
    {
        var skip = (request.Page - 1) * request.PageSize;

        var (items, total) = await _agreements.GetPagedAsync(
            providerProfileId: request.ProviderProfileId,
            productCode:       request.ProductCode,
            status:            request.Status,
            dateFrom:          request.DateFrom,
            dateTo:            request.DateTo,
            search:            request.Search,
            skip:              skip,
            take:              request.PageSize,
            ct:                ct);

        return new CargoDryConsignmentAgreementPagedResultDto
        {
            Items = items.Select(e => new CargoDryConsignmentAgreementListItemDto
            {
                Id                = e.Id,
                AgreementCode     = e.AgreementCode,
                ProviderProfileId = e.ProviderProfileId,
                ProductCode       = e.ProductCode,
                ConsignmentRate   = e.ConsignmentRate,
                CurrencyCode      = e.CurrencyCode,
                MaxKitCount       = e.MaxKitCount,
                AllocatedKitCount = e.AllocatedKitCount,
                RemainingKitCount = e.RemainingKitCount,
                Status            = e.Status,
                StatusName        = e.Status.ToString(),
                StartDateUtc      = e.StartDateUtc,
                EndDateUtc        = e.EndDateUtc,
                CreatedAt         = e.CreateDate?.ToString("O"),
            }).ToList(),
            Total    = total,
            Page     = request.Page,
            PageSize = request.PageSize,
        };
    }
}
