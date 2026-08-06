using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ChangeOrder;
using Aizen.Modules.ServiceRequest.Application.Mapping;
using Aizen.Modules.ServiceRequest.Domain.Entities.ChangeOrder;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Command.ChangeOrder;

/// <summary>
/// BE-S11b — provider proposes a change order on an accepted SR. Records the proposed lines (immutable) with status
/// Proposed. NOTHING financial happens — no snapshot, no escrow, not in any total or collection until the customer approves.
/// Guards: the referenced offer must be Accepted (there is an acceptance snapshot). Provider-scoped (the proposer is the
/// current user).
/// </summary>
[DocumentationInfo("Propose change order command handler", "Creates a Proposed change order; no economics until customer approval.")]
public sealed class ProposeServiceChangeOrderCommandHandler
    : AizenCommandHandler<ProposeServiceChangeOrderCommand, ServiceChangeOrderDto>
{
    private readonly IServiceRequestRepository        _srRepository;
    private readonly IServiceRequestOfferRepository   _offerRepository;
    private readonly IServiceChangeOrderRepository    _changeOrderRepository;
    private readonly IAizenInfoAccessor               _info;
    private readonly IAizenMessagePublisher           _messagePublisher;
    private readonly ILogger<ProposeServiceChangeOrderCommandHandler> _logger;

    public ProposeServiceChangeOrderCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestOfferRepository offerRepository,
        IServiceChangeOrderRepository changeOrderRepository, IAizenInfoAccessor info,
        IAizenMessagePublisher messagePublisher, ILogger<ProposeServiceChangeOrderCommandHandler> logger)
    {
        _srRepository = srRepository; _offerRepository = offerRepository;
        _changeOrderRepository = changeOrderRepository; _info = info;
        _messagePublisher = messagePublisher; _logger = logger;
    }

    public override async Task<ServiceChangeOrderDto?> Handle(ProposeServiceChangeOrderCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var sr = await _srRepository.GetByIdAsync(request.ServiceRequestId, ct)
            ?? throw new InvalidOperationException($"ServiceRequest {request.ServiceRequestId} not found.");
        var offer = await _offerRepository.GetByIdAsync(request.Request.AcceptedOfferId, ct)
            ?? throw new InvalidOperationException($"Offer {request.Request.AcceptedOfferId} not found.");

        // Guard — a change order only on an accepted offer (there is an acceptance snapshot). §20.13.
        if (offer.Status != ServiceRequestOfferStatus.Accepted || offer.ServiceRequestId != sr.Id)
            throw new AizenBusinessException("SR_CO_REQUIRES_ACCEPTED_OFFER");

        if (request.Request.Items is null || request.Request.Items.Count == 0)
            throw new AizenBusinessException("SR_CO_REQUIRES_LINES");

        var currentUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var currency = string.IsNullOrWhiteSpace(request.Request.Items[0].CurrencyCode)
            ? offer.CurrencyCode
            : request.Request.Items[0].CurrencyCode;

        var items = request.Request.Items
            .Select((input, idx) => ServiceChangeOrderItemEntity.Create(
                itemType:              input.ItemType,
                title:                 input.Title,
                description:           input.Description,
                quantity:              input.Quantity,
                unitPrice:             input.UnitPrice,
                currencyCode:          string.IsNullOrWhiteSpace(input.CurrencyCode) ? currency : input.CurrencyCode,
                sortOrder:             input.SortOrder ?? idx,
                unitCode:              input.UnitCode,
                taxRate:               input.TaxRate,
                discountType:          input.DiscountType,
                discountValue:         input.DiscountValue,
                pricingMethod:         input.PricingMethod,
                commissionEligibility: input.CommissionEligibility,
                discountEligibility:   input.DiscountEligibility))
            .ToList();

        var seq = await _changeOrderRepository.CountForOfferAsync(offer.Id, ct) + 1;

        var changeOrder = ServiceChangeOrderEntity.Create(
            serviceRequestId: sr.Id, acceptedOfferId: offer.Id, sequenceNo: seq,
            direction: request.Request.Direction, currencyCode: currency, reason: request.Request.Reason,
            proposedByUserId: currentUserId, items: items, utcNow: now);

        await _changeOrderRepository.AddAsync(changeOrder, ct);

        await _messagePublisher.PublishAsync(new ServiceChangeOrderProposedMessage
        {
            ServiceRequestId  = sr.Id,
            ChangeOrderId     = changeOrder.Id,
            AcceptedOfferId   = offer.Id,
            OwnerUserId       = sr.OwnerUserId,
            ProviderProfileId = offer.ProviderProfileId,
            Direction         = changeOrder.Direction,
        }, ct);

        _logger.LogInformation(
            "Change order proposed for SR {SrId} Offer {OfferId}: CO {CoId} seq {Seq} {Direction}, {Count} line(s). No economics yet.",
            sr.Id, offer.Id, changeOrder.Id, seq, changeOrder.Direction, items.Count);

        return changeOrder.ToDto();
    }
}
