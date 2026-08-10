using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Application.Configuration;
using Aizen.Modules.ServiceRequest.Application.Services;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;
using Microsoft.Extensions.Options;

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
    private readonly IServiceRequestMessageRepository _messageRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly OfferCalculationService _calculation;
    private readonly UnitCodeValidator _unitCodeValidator;
    private readonly IAizenMessagePublisher _messagePublisher;
    private readonly Services.Fx.OfferFxResolver _fxResolver;
    private readonly IOptionsMonitor<MessagingWriteCutoverOptions> _cutover;

    public SubmitOfferCommandHandler(
        IServiceRequestRepository srRepository,
        IServiceRequestOfferRepository offerRepository,
        IServiceRequestMessageRepository messageRepository,
        IAizenInfoAccessor info,
        OfferCalculationService calculation,
        UnitCodeValidator unitCodeValidator,
        IAizenMessagePublisher messagePublisher,
        Services.Fx.OfferFxResolver fxResolver,
        IOptionsMonitor<MessagingWriteCutoverOptions> cutover)
    {
        _srRepository = srRepository;
        _offerRepository = offerRepository;
        _messageRepository = messageRepository;
        _info = info;
        _calculation = calculation;
        _unitCodeValidator = unitCodeValidator;
        _messagePublisher = messagePublisher;
        _fxResolver = fxResolver;
        _cutover = cutover;
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

        // BE-S3a: resolve FX at the single submit instant and convert any foreign lines to TRY BEFORE the economics runs.
        // Fail-loud (SR_FX_RATE_UNAVAILABLE) if a source currency has no effective rate. TRY-only offers resolve nothing.
        var submitInstant = DateTime.UtcNow;
        await _fxResolver.ResolveAndConvertAsync(offer, submitInstant, ct);

        // Recalculate before submitting (runs on the now-TRY unit prices — the 8-equality is single-currency as always)
        _calculation.Calculate(offer);

        offer.MarkSubmitted(submitInstant);
        _offerRepository.Update(offer);

        // Update SR status if needed
        if (sr.Status == ServiceRequestStatus.Open || sr.Status == ServiceRequestStatus.WaitingForOffer)
        {
            sr.ChangeStatus(ServiceRequestStatus.OfferReceived);
            _srRepository.Update(sr);
        }

        // Create offer-as-message (idempotent per offer id)
        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;

        // BE_WC1 — first-class submit event (ALWAYS published) → Messaging generates the offer card. Dedicated to the
        // card (NOT the draft-time ServiceRequestOfferCreatedMessage, which the Notification module consumes), so no
        // new notification fires and the card carries the final submit-time total.
        await _messagePublisher.PublishAsync(new ServiceRequestOfferSubmittedMessage
        {
            ServiceRequestId = sr.Id, OfferId = offer.Id, ProviderProfileId = offer.ProviderProfileId,
            ProviderUserId = currentUserId, TotalAmount = offer.GrandTotal, CurrencyCode = offer.CurrencyCode,
            OccurredAt = DateTimeOffset.UtcNow,
        }, ct);

        // BE_WC1 flag-gated: when SystemMessages is ON, Messaging owns the offer card (from the event above), so the SR
        // module stops writing the sr.Messages offer row + the chat-mirror event.
        var hasOfferMsg = await _messageRepository.HasOfferMessageForOfferAsync(sr.Id, offer.Id, ct);
        if (!_cutover.CurrentValue.SystemMessages && !hasOfferMsg)
        {
            var offerMessage = ServiceRequestMessageEntity.Create(
                sr.Id, currentUserId, ServiceRequestMessageSenderType.Provider,
                ServiceRequestMessageType.Offer,
                $"offer:{offer.Id}|{offer.GrandTotal:F2} {offer.CurrencyCode}",
                null);
            await _messageRepository.AddAsync(offerMessage, ct);

            // Phase-2 live-sync: publish the (enriched) event so the offer message mirrors into the Messaging store.
            // The provider realtime mapper ignores Provider-sender events (no self-notify); Notification doesn't consume this.
            await _messagePublisher.PublishAsync(new ServiceRequestMessageSentMessage
            {
                ServiceRequestId = sr.Id,
                MessageId = offerMessage.Id,
                SenderUserId = currentUserId,
                SenderType = ServiceRequestMessageSenderType.Provider,
                ProviderProfileId = offer.ProviderProfileId,
                Content = offerMessage.Content,
                MessageType = offerMessage.MessageType,
                OccurredAt = DateTimeOffset.UtcNow,
            }, ct);
        }

        return new SubmitOfferResponse(offer.ToDto());
    }
}
