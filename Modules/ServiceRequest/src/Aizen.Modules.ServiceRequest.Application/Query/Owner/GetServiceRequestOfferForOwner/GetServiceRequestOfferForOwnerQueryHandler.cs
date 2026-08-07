using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Query.Owner.GetServiceRequestOfferForOwner;

/// <summary>
/// One received offer for the caller-owner's SR, with its full cost-free item breakdown (items include S3 FX).
/// Owner-scoped (caller must own the SR); the offer must belong to that SR and not be a Draft. Cost-free.
/// </summary>
public sealed class GetServiceRequestOfferForOwnerQueryHandler
    : AizenQueryHandler<GetServiceRequestOfferForOwnerQuery, GetServiceRequestOfferForOwnerResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IAizenInfoAccessor _info;

    public GetServiceRequestOfferForOwnerQueryHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestOfferRepository offerRepository,
        IAizenInfoAccessor info)
    {
        _srRepository = srRepository;
        _offerRepository = offerRepository;
        _info = info;
    }

    public override async Task<GetServiceRequestOfferForOwnerResponse?> Handle(
        GetServiceRequestOfferForOwnerQuery request, CancellationToken ct)
    {
        var ownerUserId = _info.UserInfoAccessor.UserInfo.UserId;

        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, ct);
        if (sr is null || sr.OwnerUserId != ownerUserId)
            throw new AizenBusinessException("Service request not found.");

        // GetByIdAsync includes Items + FxSnapshots — the full breakdown.
        var offer = await _offerRepository.GetByIdAsync(request.OfferId, ct);
        if (offer is null || offer.ServiceRequestId != sr.Id || offer.Status == ServiceRequestOfferStatus.Draft)
            throw new AizenBusinessException("Offer not found.");

        return new GetServiceRequestOfferForOwnerResponse { Offer = offer.ToDto() };
    }
}
