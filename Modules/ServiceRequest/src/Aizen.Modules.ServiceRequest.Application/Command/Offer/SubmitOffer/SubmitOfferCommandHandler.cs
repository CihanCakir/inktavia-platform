using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Application.Services;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer.SubmitOffer;

/// <summary>
/// Draft → Submitted. Idempotent: same idempotency key returns the same offer, not a second one.
/// Rejects empty offers (no priced lines). Rejects closed requests.
/// </summary>
public sealed class SubmitOfferCommandHandler : AizenCommandHandler<SubmitOfferCommand, SubmitOfferResponse>
{
    private static readonly HashSet<ServiceRequestStatus> BiddableStatuses = new()
    {
        ServiceRequestStatus.Open,
        ServiceRequestStatus.WaitingForOffer,
        ServiceRequestStatus.OfferReceived,
    };

    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly OfferCalculationService _calculation;
    private readonly UnitCodeValidator _unitCodeValidator;

    public SubmitOfferCommandHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestOfferRepository offerRepository,
        IAizenInfoAccessor info,
        OfferCalculationService calculation,
        UnitCodeValidator unitCodeValidator)
    {
        _srRepository = srRepository;
        _offerRepository = offerRepository;
        _info = info;
        _calculation = calculation;
        _unitCodeValidator = unitCodeValidator;
    }

    public override async Task<SubmitOfferResponse?> Handle(SubmitOfferCommand command, CancellationToken ct)
    {
        var providerProfileId = _info.KeycloakTokenInfoAccessor.KeycloakTokenInfo?.ProviderProfileId ?? 0;
        if (providerProfileId <= 0)
            throw new AizenBusinessException("Provider identity could not be resolved.");

        var sr = await _srRepository.GetByIdAsync(command.ServiceRequestId, ct)
            ?? throw new AizenBusinessException("Service request not found.");

        if (!BiddableStatuses.Contains(sr.Status))
            throw new AizenBusinessException("SR_OFFER_REQUEST_CLOSED");

        var offer = await _offerRepository.GetByIdAsync(command.OfferId, ct)
            ?? throw new AizenBusinessException("Offer not found.");

        if (offer.ProviderProfileId != providerProfileId)
            throw new AizenBusinessException("Offer not found.");

        // Idempotency: if already submitted, return it
        if (offer.Status == ServiceRequestOfferStatus.Submitted)
            return new SubmitOfferResponse(offer.ToDto());

        if (offer.Status != ServiceRequestOfferStatus.Draft)
            throw new AizenBusinessException("SR_OFFER_ALREADY_SUBMITTED");

        // Validate unit codes on persisted items
        var itemsForValidation = offer.Items
            .Where(i => !i.IsDeleted && !string.IsNullOrWhiteSpace(i.UnitCode))
            .Select(i => new Abstraction.Request.Offer.CreateServiceRequestOfferItemRequest { UnitCode = i.UnitCode, Title = i.Title })
            .ToList();
        if (itemsForValidation.Count > 0)
            await _unitCodeValidator.ValidateUnitCodesAsync(itemsForValidation, ct);

        // Reject empty offers (no priced lines)
        var pricedLines = offer.Items.Where(i => i.ItemType != ServiceRequestOfferItemType.Discount && !i.IsDeleted).ToList();
        if (pricedLines.Count == 0)
            throw new AizenBusinessException("SR_OFFER_EMPTY");

        // Recalculate before submitting
        _calculation.Calculate(offer);

        offer.MarkSubmitted(DateTime.UtcNow);
        _offerRepository.Update(offer);

        // Update SR status if needed
        if (sr.Status == ServiceRequestStatus.Open || sr.Status == ServiceRequestStatus.WaitingForOffer)
        {
            sr.ChangeStatus(ServiceRequestStatus.OfferReceived);
            _srRepository.Update(sr);
        }

        return new SubmitOfferResponse(offer.ToDto());
    }
}
