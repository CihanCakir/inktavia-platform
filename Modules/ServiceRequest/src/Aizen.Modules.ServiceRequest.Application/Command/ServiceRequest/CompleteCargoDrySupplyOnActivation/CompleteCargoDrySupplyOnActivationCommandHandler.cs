using Aizen.Core.CQRS.Handler;
using Aizen.Core.InfoAccessor.Abstraction;
using Aizen.Modules.CargoDry.Abstraction.RemoteCall;
using Aizen.Modules.CargoDry.Abstraction.RemoteCall.Requests;
using Aizen.Modules.Payment.Abstraction.RemoteCall;
using Aizen.Modules.Payment.Abstraction.RemoteCall.Requests;
using Aizen.Modules.ServiceRequest.Abstraction.Enum;
using Aizen.Modules.ServiceRequest.Abstraction.Response.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Aizen.Modules.ServiceRequest.Domain.Interface.Repository;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.ServiceRequest.Application.Command.ServiceRequest.CompleteCargoDrySupplyOnActivation;

[DocumentationInfo("Complete CargoDry supply on activation command handler",
    "Correlates a kit activation with the owner's oldest open CARGODRY_SUPPLY SR (vessel+product); on a match releases " +
    "the platform-collected escrow, completes+closes the SR, and records the sale in CargoDry (attribution + preferred " +
    "provider). Walk-in / product mismatch = safe no-op. Idempotent via the SR-status correlation gate + per-SR sale key.")]
public sealed class CompleteCargoDrySupplyOnActivationCommandHandler
    : AizenCommandHandler<CompleteCargoDrySupplyOnActivationCommand, CompleteCargoDrySupplyOnActivationResponse>
{
    private readonly IServiceRequestRepository  _repository;
    private readonly IAizenInfoAccessor         _info;
    private readonly IPaymentModuleRemoteCall   _payment;
    private readonly ICargoDrySupplyRemoteCall  _cargoDry;
    private readonly ILogger<CompleteCargoDrySupplyOnActivationCommandHandler> _logger;

    public CompleteCargoDrySupplyOnActivationCommandHandler(
        IServiceRequestRepository  repository,
        IAizenInfoAccessor         info,
        IPaymentModuleRemoteCall   payment,
        ICargoDrySupplyRemoteCall  cargoDry,
        ILogger<CompleteCargoDrySupplyOnActivationCommandHandler> logger)
    {
        _repository = repository;
        _info       = info;
        _payment    = payment;
        _cargoDry   = cargoDry;
        _logger     = logger;
    }

    public override async Task<CompleteCargoDrySupplyOnActivationResponse?> Handle(
        CompleteCargoDrySupplyOnActivationCommand request, CancellationToken ct)
    {
        var ownerUserId = _info.UserInfoAccessor.UserInfo.UserId;
        var rawToken    = _info.UserInfoAccessor.UserInfo.AccessToken;

        // ── Correlate: oldest open supply SR for owner+vessel+product. None ⇒ walk-in / product-vessel mismatch → no-op. ──
        var sr = await _repository.GetOldestOpenCargoDrySupplyAsync(ownerUserId, request.VesselId, request.ProductCode, ct);
        if (sr is null)
        {
            _logger.LogInformation(
                "CargoDry activation (kit {KitId}, vessel {VesselId}, product {Product}) matched no open CARGODRY_SUPPLY SR — no-op (walk-in/mismatch).",
                request.KitId, request.VesselId, request.ProductCode);
            return new CompleteCargoDrySupplyOnActivationResponse
            {
                Correlated = false, Note = "No open CARGODRY_SUPPLY SR for owner+vessel+product.",
            };
        }

        var prevStatus = sr.Status;

        // ── Release the platform-collected escrow (no provider payout — platform-only branch). Best-effort: if it fails
        //    we still complete the SR and reconcile the release later, mirroring ReleasePaymentCommandHandler. ──
        var released = false;
        if (sr.PaymentTransactionId is { } txId)
        {
            try
            {
                await _payment.ReleaseEscrowAsync(
                    txId,
                    new ReleaseEscrowRemoteCallRequest
                    {
                        ApprovedByUserId = ownerUserId,
                        AdminNote        = $"CargoDry kit {request.KitId} activated (SR {sr.Id})",
                    },
                    $"Bearer {rawToken}", ct);
                released = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Escrow release failed for CargoDry supply SR {SrId} tx {TxId}; completing SR anyway (reconcile later).",
                    sr.Id, txId);
            }
        }

        // ── Complete + close the SR (activation == completion for a supply request; no work-completion review). ──
        sr.MarkCompleted(DateTimeOffset.UtcNow);
        sr.AddStatusHistory(ServiceRequestStatusHistoryEntity.Create(
            sr.Id, prevStatus, ServiceRequestStatus.Completed,
            "CargoDry kit activated", ownerUserId, ServiceRequestActorType.Owner));
        sr.ReleasePayment(); // → Closed
        sr.AddStatusHistory(ServiceRequestStatusHistoryEntity.Create(
            sr.Id, ServiceRequestStatus.Completed, ServiceRequestStatus.Closed,
            "CargoDry supply escrow released to platform", ownerUserId, ServiceRequestActorType.Owner));
        _repository.Update(sr);

        // ── Record the sale in CargoDry (attribution enrichment + preferred provider). SaleAmount = pinned retail from
        //    the accepted offer. Idempotent per SR. Best-effort: SR is already closed; a failure is reconcilable. ──
        var recorded = false;
        var acceptedOffer = sr.Offers.FirstOrDefault(o => o.Status == ServiceRequestOfferStatus.Accepted)
                            ?? sr.Offers.OrderByDescending(o => o.Id).FirstOrDefault();
        if (acceptedOffer is not null)
        {
            try
            {
                var result = await _cargoDry.RecordSaleAsync(
                    new RecordCargoDrySupplySaleRemoteRequest
                    {
                        KitId            = request.KitId,
                        ServiceRequestId = sr.Id,
                        OwnerUserId      = ownerUserId,
                        SaleAmount       = acceptedOffer.GrandTotal,
                        CurrencyCode     = acceptedOffer.CurrencyCode,
                        ResolvedByUserId = ownerUserId,
                    },
                    $"Bearer {rawToken}", ct);
                recorded = result.Recorded;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CargoDry record-sale failed for supply SR {SrId}; SR completed — attribution reconcile later.", sr.Id);
            }
        }
        else
        {
            _logger.LogWarning("CargoDry supply SR {SrId} has no accepted offer at activation — sale not recorded.", sr.Id);
        }

        _logger.LogInformation(
            "CargoDry supply SR {SrId} completed on kit {KitId} activation. EscrowReleased={Released} SaleRecorded={Recorded}",
            sr.Id, request.KitId, released, recorded);

        return new CompleteCargoDrySupplyOnActivationResponse
        {
            Correlated = true, ServiceRequestId = sr.Id, EscrowReleased = released, SaleRecorded = recorded,
        };
    }
}
