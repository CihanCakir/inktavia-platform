using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.Payment.Abstraction.Enum;
using PaymentRoot = Aizen.Modules.Payment.Abstraction;
using Aizen.Modules.Payment.Abstraction.Model;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Model;
using Aizen.Modules.ServiceRequest.Abstraction.Response.Offer;
using Aizen.Modules.ServiceRequest.Application.Realtime;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Aizen.Modules.ServiceRequest.Repository.Mapping;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Command.Offer;

[DocumentationInfo("Accept offer command handler", "Marks the offer as accepted, updates service request to OfferAccepted, creates payment escrow, publishes realtime event.")]
public sealed class AcceptServiceRequestOfferCommandHandler : AizenCommandHandler<AcceptServiceRequestOfferCommand, AcceptServiceRequestOfferResponse>
{
    private readonly IServiceRequestRepository _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IAizenInfoAccessor _info;
    private readonly ServiceRequestRealtimePublisher _realtimePublisher;
    private readonly IPaymentModuleRemoteCall _paymentRemoteCall;
    private readonly ILogger<AcceptServiceRequestOfferCommandHandler> _logger;

    public AcceptServiceRequestOfferCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestOfferRepository offerRepository,
        IAizenInfoAccessor info, ServiceRequestRealtimePublisher realtimePublisher,
        IPaymentModuleRemoteCall paymentRemoteCall,
        ILogger<AcceptServiceRequestOfferCommandHandler> logger)
    {
        _srRepository = srRepository; _offerRepository = offerRepository;
        _info = info; _realtimePublisher = realtimePublisher;
        _paymentRemoteCall = paymentRemoteCall; _logger = logger;
    }

    public override async Task<AcceptServiceRequestOfferResponse?> Handle(AcceptServiceRequestOfferCommand request, CancellationToken cancellationToken)
    {
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, cancellationToken)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");
        var offer = await _offerRepository.GetByIdAsync(request.Request.OfferId, cancellationToken)
            ?? throw new InvalidOperationException($"Offer {request.Request.OfferId} not found.");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var rawToken     = _info.UserInfoAccessor.UserInfo.AccessToken; // forwarded to Payment module
        var prevStatus   = sr.Status;

        offer.Accept();
        _offerRepository.Update(offer);

        sr.ChangeStatus(ServiceRequestStatus.OfferAccepted);
        var history = ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.OfferAccepted,
            "Offer accepted", currentUserId, ServiceRequestActorType.Owner);
        sr.AddStatusHistory(history);

        // ── Payment escrow creation ──────────────────────────────────────────
        // IdempotencyKey guarantees no double-charge on retry.
        // PayerProfileId = OwnerUserId (MVP assumption: userId == profileId for owners)
        // Post-MVP: resolve actual ParticipantProfileId from Identity module.
        try
        {
            var escrowRequest = new CreateEscrowRemoteCallRequest
            {
                IdempotencyKey     = $"SR-{sr.Id}-OFFER-{offer.Id}",
                Context            = TransactionContext.ForServiceRequest(sr.Id, offer.Id),
                TransactionType    = PaymentRoot.TransactionType.ServiceRequestEscrow,
                PayerProfileId     = sr.OwnerUserId,          // MVP: userId as profileId
                RecipientProfileId = offer.ProviderProfileId,
                GrossAmount        = offer.TotalAmount,
                DiscountAmount     = 0m,
                CurrencyCode       = offer.CurrencyCode,
                ProviderPlanId     = null,                     // resolved dynamically in Payment module
                CategoryCode       = sr.ServiceCategoryCode,
                EscrowRequired     = true,
            };

            var escrowResult = await _paymentRemoteCall.CreateEscrowAsync(
                escrowRequest,
                $"Bearer {rawToken}",
                cancellationToken);

            sr.SetPaymentTransaction(escrowResult.TransactionId);

            _logger.LogInformation(
                "Escrow created for SR {SrId} Offer {OfferId}: TransactionId={TxId} Amount={Amount} {Currency}",
                sr.Id, offer.Id, escrowResult.TransactionId, offer.TotalAmount, offer.CurrencyCode);
        }
        catch (Exception ex)
        {
            // Log and continue — escrow failure does not roll back offer acceptance in MVP.
            // Post-MVP: implement compensation (reject offer if escrow fails).
            _logger.LogError(ex,
                "Escrow creation failed for SR {SrId} Offer {OfferId}. Offer accepted but no escrow held.",
                sr.Id, offer.Id);
        }

        _srRepository.Update(sr);

        await _realtimePublisher.PublishAsync(sr.Id, sr.RequestCode, sr.OwnerUserId, offer.ProviderProfileId,
            ServiceRequestRealtimeEventType.OfferAccepted, offer.ToDto(),
            currentUserId, ServiceRequestActorType.Owner, cancellationToken);

        return new AcceptServiceRequestOfferResponse(offer.Id, sr.Id);
    }
}
