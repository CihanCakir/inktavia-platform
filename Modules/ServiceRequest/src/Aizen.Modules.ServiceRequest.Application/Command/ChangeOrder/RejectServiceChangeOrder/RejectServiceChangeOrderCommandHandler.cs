using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Core.Messagebus.Abstraction.Senders;
using Aizen.Modules.ServiceRequest.Abstraction.Message;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ChangeOrder;
using Aizen.Modules.ServiceRequest.Application.Mapping;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Command.ChangeOrder;

/// <summary>
/// BE-S11b — customer rejects a proposed change order. Terminal, no economics (nothing was ever charged). Exercisable via
/// API/bus without an owner app. Idempotent-ish: a re-reject of an already-rejected order returns it unchanged.
/// </summary>
[DocumentationInfo("Reject change order command handler", "Rejects a proposed change order (terminal, no economics).")]
public sealed class RejectServiceChangeOrderCommandHandler
    : AizenCommandHandler<RejectServiceChangeOrderCommand, ServiceChangeOrderDto>
{
    private readonly IServiceRequestRepository      _srRepository;
    private readonly IServiceRequestOfferRepository _offerRepository;
    private readonly IServiceChangeOrderRepository  _changeOrderRepository;
    private readonly IAizenMessagePublisher         _messagePublisher;
    private readonly ILogger<RejectServiceChangeOrderCommandHandler> _logger;

    public RejectServiceChangeOrderCommandHandler(
        IServiceRequestRepository srRepository, IServiceRequestOfferRepository offerRepository,
        IServiceChangeOrderRepository changeOrderRepository, IAizenMessagePublisher messagePublisher,
        ILogger<RejectServiceChangeOrderCommandHandler> logger)
    {
        _srRepository = srRepository; _offerRepository = offerRepository;
        _changeOrderRepository = changeOrderRepository; _messagePublisher = messagePublisher; _logger = logger;
    }

    public override async Task<ServiceChangeOrderDto?> Handle(RejectServiceChangeOrderCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var co = await _changeOrderRepository.GetByIdAsync(request.ChangeOrderId, ct)
            ?? throw new InvalidOperationException($"Change order {request.ChangeOrderId} not found.");
        if (co.ServiceRequestId != request.ServiceRequestId)
            throw new AizenBusinessException("SR_CO_MISMATCH");

        if (co.Status == Abstraction.Enum.ServiceChangeOrderStatus.Rejected)
            return co.ToDto();

        co.Reject(request.Request?.Reason, now);
        _changeOrderRepository.Update(co);

        var sr = await _srRepository.GetByIdAsync(co.ServiceRequestId, ct);
        var offer = await _offerRepository.GetByIdAsync(co.AcceptedOfferId, ct);
        if (sr is not null && offer is not null)
        {
            await _messagePublisher.PublishAsync(new ServiceChangeOrderRejectedMessage
            {
                ServiceRequestId  = sr.Id,
                ChangeOrderId     = co.Id,
                AcceptedOfferId   = offer.Id,
                OwnerUserId       = sr.OwnerUserId,
                ProviderProfileId = offer.ProviderProfileId,
                Reason            = request.Request?.Reason,
            }, ct);
        }

        _logger.LogInformation("Change order {CoId} rejected by customer for SR {SrId}.", co.Id, co.ServiceRequestId);
        return co.ToDto();
    }
}
