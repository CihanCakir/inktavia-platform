using Aizen.Core.CQRS.Handler;
using Aizen.Modules.CargoDry.Application.Commands.ResolveCargoDrySalesAttributionFinancials;
using Aizen.Modules.CargoDry.Domain.Entities;
using Aizen.Modules.CargoDry.Domain.Interface.Repository;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.CargoDry.Application.Commands.RecordCargoDrySupplySale;

[DocumentationInfo("Record CargoDry supply sale command handler",
    "Enriches the kit-activation attribution with the retail sale amount + agreement commission (via the existing " +
    "ResolveCargoDrySalesAttributionFinancials cascade → SellThroughSettlement roll-up) and records the owner's " +
    "preferred provider on the first completed supply SR. Idempotent per source SR — never double-credits.")]
public sealed class RecordCargoDrySupplySaleCommandHandler
    : AizenCommandHandler<RecordCargoDrySupplySaleCommand, RecordCargoDrySupplySaleResponse>
{
    private readonly ICargoDrySalesAttributionRepository       _attributions;
    private readonly ICargoDryOwnerPreferredProviderRepository _preferred;
    private readonly ISender                                   _sender;
    private readonly ILogger<RecordCargoDrySupplySaleCommandHandler> _logger;

    public RecordCargoDrySupplySaleCommandHandler(
        ICargoDrySalesAttributionRepository       attributions,
        ICargoDryOwnerPreferredProviderRepository preferred,
        ISender                                   sender,
        ILogger<RecordCargoDrySupplySaleCommandHandler> logger)
    {
        _attributions = attributions;
        _preferred    = preferred;
        _sender       = sender;
        _logger       = logger;
    }

    public override async Task<RecordCargoDrySupplySaleResponse?> Handle(
        RecordCargoDrySupplySaleCommand request, CancellationToken ct)
    {
        // ── Idempotency (keyed by source SR) — a re-fired activation can never double-credit the provider ──
        var already = await _attributions.GetBySourceServiceRequestIdAsync(request.ServiceRequestId, ct);
        if (already is not null)
        {
            _logger.LogInformation(
                "CargoDry supply sale already recorded for SR {SrId} (attribution {AttrId}) — no-op.",
                request.ServiceRequestId, already.Id);
            return new RecordCargoDrySupplySaleResponse
            {
                Recorded = false, AttributionId = already.Id, Note = "Already recorded for this SR.",
            };
        }

        // ── Locate the attribution created at kit activation (ResolveAsync). Absent ⇒ walk-in / unattributed kit. ──
        var attribution = await _attributions.GetByKitIdAsync(request.KitId, ct);
        if (attribution is null)
        {
            _logger.LogWarning(
                "No attribution for kit {KitId} on supply SR {SrId} — nothing to enrich (walk-in/unattributed).",
                request.KitId, request.ServiceRequestId);
            return new RecordCargoDrySupplySaleResponse
            {
                Recorded = false, Note = "No attribution for kit; nothing to enrich.",
            };
        }

        // ── Link SR (set-once) then enrich financials via the EXISTING cascade (agreement rate → provider share,
        //    settlement totals recalculated). Do NOT fork settlement math. The inner command's SaveChanges persists
        //    the link too (same tracked entity, same DbContext scope). ──
        attribution.LinkSupplyServiceRequest(request.ServiceRequestId);

        await _sender.Send(new ResolveCargoDrySalesAttributionFinancialsCommand
        {
            SalesAttributionId = attribution.Id,
            SalePrice          = request.SaleAmount,
            CurrencyCode       = request.CurrencyCode,
            ResolvedByUserId   = request.ResolvedByUserId,
            ResolutionNote     = $"CargoDry supply sale (SR {request.ServiceRequestId})",
        }, ct);

        // ── Preferred provider (set-once on the owner's FIRST completed supply SR); only when a provider is attributed ──
        var preferredSet = false;
        if (attribution.ProviderProfileId is { } providerProfileId)
        {
            var existingPref = await _preferred.GetByOwnerAsync(request.OwnerUserId, ct);
            if (existingPref is null)
            {
                await _preferred.AddAsync(
                    CargoDryOwnerPreferredProviderEntity.Create(
                        request.OwnerUserId, providerProfileId, request.ServiceRequestId, DateTime.UtcNow),
                    ct);
                await _preferred.SaveChangesAsync(ct);
                preferredSet = true;
            }
        }

        _logger.LogInformation(
            "CargoDry supply sale recorded. SR={SrId} Kit={KitId} Attribution={AttrId} Sale={Sale} {Currency} PreferredSet={Pref}",
            request.ServiceRequestId, request.KitId, attribution.Id, request.SaleAmount, request.CurrencyCode, preferredSet);

        return new RecordCargoDrySupplySaleResponse
        {
            Recorded = true, AttributionId = attribution.Id, PreferredProviderSet = preferredSet,
        };
    }
}
