using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Owner;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Query.Owner.GetServiceRequestOffersForOwner;

/// <summary>
/// Returns the offers a provider submitted on the caller's own service request. Owner-scoped like the provider
/// detail access-check: the caller must own the SR (OwnerUserId == the trusted UserInfo.UserId), else a clean
/// not-found (no existence leak). Excludes other providers' Drafts. Cost-free: the offer DTO carries customer
/// totals + line items + S3 FX only — never provider cost / commission / funding.
/// </summary>
public sealed class GetServiceRequestOffersForOwnerQueryHandler
    : AizenQueryHandler<GetServiceRequestOffersForOwnerQuery, GetServiceRequestOffersForOwnerResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IAizenInfoAccessor _info;

    public GetServiceRequestOffersForOwnerQueryHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestOfferRepository offerRepository,
        IAizenInfoAccessor info)
    {
        _srRepository = srRepository;
        _offerRepository = offerRepository;
        _info = info;
    }

    public override async Task<GetServiceRequestOffersForOwnerResponse?> Handle(
        GetServiceRequestOffersForOwnerQuery request, CancellationToken ct)
    {
        var ownerUserId = _info.UserInfoAccessor.UserInfo.UserId;

        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, ct);
        if (sr is null || sr.OwnerUserId != ownerUserId)
            throw new AizenBusinessException("Service request not found.");

        var offers = await _offerRepository.GetByServiceRequestIdAsync(sr.Id, ct);

        // Owner-visible only: exclude other providers' unsubmitted Drafts. Submitted/UnderReview/Accepted/
        // Rejected/Withdrawn/Expired all legitimately belong to the owner's inbox.
        var visible = offers
            .Where(o => o.Status != ServiceRequestOfferStatus.Draft)
            .Select(o => o.ToDto())
            .ToList();

        return new GetServiceRequestOffersForOwnerResponse { Offers = visible };
    }
}
