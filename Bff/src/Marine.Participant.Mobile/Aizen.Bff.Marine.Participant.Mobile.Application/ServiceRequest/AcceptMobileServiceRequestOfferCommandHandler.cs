using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.ServiceRequest;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ServiceRequest.Abstraction.Request.Offer;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.ServiceRequest;

/// <summary>
/// The owner accepts a received offer → money moves (BE_MO3). The module accept runs BE-P8 (plan → S7 → P3 fee →
/// P5 gate → S8 snapshot → escrow) and captures at accept via the DI-selected gateway (manual in dev; iyzico when
/// the P9 sandbox keys are present), all in ONE transactional command — so a Rejected/ConfigError acceptance
/// throws and leaves NO half-accepted, unpaid SR. The module accept does NOT owner-check, so the BFF gates the SR
/// against the resolved owner id (EnsureOwnedAsync) before proxying. After accept it reads the payment status so
/// the client can show the result immediately (dev/manual → already Paid) while still being free to poll for the
/// asynchronous live-iyzico 3DS path. Cost-free: only the customer total + lifecycle status cross.
/// </summary>
public sealed class AcceptMobileServiceRequestOfferCommandHandler
    : AizenCommandHandler<AcceptMobileServiceRequestOfferCommand, MobileAcceptOfferResultDto>
{
    private readonly IParticipantProfileResolver _resolver;
    private readonly IParticipantIdentityHolder _holder;
    private readonly IServiceRequestRemoteCall _sr;
    private readonly ILogger<AcceptMobileServiceRequestOfferCommandHandler> _logger;

    public AcceptMobileServiceRequestOfferCommandHandler(
        IParticipantProfileResolver resolver,
        IParticipantIdentityHolder holder,
        IServiceRequestRemoteCall sr,
        ILogger<AcceptMobileServiceRequestOfferCommandHandler> logger)
    {
        _resolver = resolver;
        _holder = holder;
        _sr = sr;
        _logger = logger;
    }

    public override async Task<MobileAcceptOfferResultDto?> Handle(
        AcceptMobileServiceRequestOfferCommand request, CancellationToken cancellationToken)
    {
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        // The module accept trusts the caller — gate ownership here (clean not-found on a foreign/unknown SR).
        await MobileServiceRequestMapper.EnsureOwnedAsync(
            _sr, request.ServiceRequestId, _holder.UserId ?? 0, cancellationToken);

        // Proxy the accept. The module recomputes economics server-side and captures at accept — the body only
        // identifies the offer (no client-supplied amount / provider id). A blocked accept propagates as a business
        // error (the FE surfaces it and lets the owner retry with another offer).
        var acceptResp = await _sr.AcceptOwnerOffer(
            request.ServiceRequestId, request.OfferId, new AcceptServiceRequestOfferRequest { OfferId = request.OfferId });

        var accepted = acceptResp?.Body is not null;
        if (!accepted)
            throw new AizenBusinessException("Offer could not be accepted.");

        // Read the payment status straight after accept so the client shows the result without a mandatory poll.
        // Best-effort — a status-read hiccup must not mask the successful accept (the FE can poll payment-status).
        MobilePaymentStatusDto payment;
        try
        {
            var statusResp = await _sr.GetOwnerPaymentStatus(request.ServiceRequestId);
            payment = MobileServiceRequestMapper.MapPaymentStatus(request.ServiceRequestId, statusResp?.Body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Accept succeeded for SR {SrId} offer {OfferId} but the immediate payment-status read failed; the client will poll.",
                request.ServiceRequestId, request.OfferId);
            payment = new MobilePaymentStatusDto { ServiceRequestId = request.ServiceRequestId, HasPayment = true, Status = "Pending" };
        }

        return new MobileAcceptOfferResultDto
        {
            OfferId = request.OfferId,
            ServiceRequestId = request.ServiceRequestId,
            Accepted = true,
            Payment = payment,
        };
    }
}
