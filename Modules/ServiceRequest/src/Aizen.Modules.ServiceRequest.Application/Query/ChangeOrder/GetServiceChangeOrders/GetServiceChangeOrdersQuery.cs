using Aizen.Core.CQRS.Handler;
using Aizen.Core.CQRS.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ChangeOrder;
using Aizen.Modules.ServiceRequest.Application.Mapping;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;

namespace Aizen.Modules.ServiceRequest.Application.Query.ChangeOrder;

/// <summary>BE-S11b — the SR's change orders + the DERIVED effective total (original acceptance + Σ applied change orders).</summary>
[DocumentationInfo("Get service change orders query", "Lists an SR's change orders and derives the effective total.")]
public sealed class GetServiceChangeOrdersQuery : AizenQuery<ServiceChangeOrderListDto>
{
    public long ServiceRequestId { get; }
    public GetServiceChangeOrdersQuery(long serviceRequestId) => ServiceRequestId = serviceRequestId;
}

[DocumentationInfo("Get service change orders query handler", "Projects change orders + computes original + Σ applied deltas.")]
public sealed class GetServiceChangeOrdersQueryHandler
    : AizenQueryHandler<GetServiceChangeOrdersQuery, ServiceChangeOrderListDto>
{
    private readonly IServiceChangeOrderRepository  _changeOrderRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;

    public GetServiceChangeOrdersQueryHandler(
        IServiceChangeOrderRepository changeOrderRepository, IServiceRequestOfferRepository offerRepository)
    {
        _changeOrderRepository = changeOrderRepository; _offerRepository = offerRepository;
    }

    public override async Task<ServiceChangeOrderListDto?> Handle(GetServiceChangeOrdersQuery request, CancellationToken ct)
    {
        var cos = await _changeOrderRepository.GetByServiceRequestIdAsync(request.ServiceRequestId, ct);

        // Original acceptance total = the accepted offer's grand total (never mutated by a change order).
        decimal originalTotal = 0m;
        var offers = await _offerRepository.GetByServiceRequestIdAsync(request.ServiceRequestId, ct);
        var acceptedOffer = offers.FirstOrDefault(o => o.Status == ServiceRequestOfferStatus.Accepted);
        if (acceptedOffer is not null)
            originalTotal = acceptedOffer.GrandTotal;

        var appliedDelta = cos
            .Where(c => c.Status == ServiceChangeOrderStatus.Applied)
            .Sum(c => c.EffectiveTotalDelta);

        return new ServiceChangeOrderListDto
        {
            ServiceRequestId = request.ServiceRequestId,
            OriginalTotal    = originalTotal,
            AppliedDelta     = appliedDelta,
            EffectiveTotal   = originalTotal + appliedDelta,
            ChangeOrders     = cos.Select(c => c.ToDto()).ToList(),
        };
    }
}
