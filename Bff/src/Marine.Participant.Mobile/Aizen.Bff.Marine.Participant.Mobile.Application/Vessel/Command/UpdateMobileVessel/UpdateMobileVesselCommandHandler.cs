using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Vessel.Abstraction.Dto.Engine;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Abstraction.Request.Engine;
using Aizen.Modules.Vessel.Abstraction.Request.Specification;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// Update orchestration (M4c). Mirrors the M4b create composite: the module has no cross-call transaction, so
/// this composes the module's owner-gated writes — update-core (required) → upsert-spec → update-engine.
///
/// The module handlers do a FULL update (unspecified fields are nulled), so this applies PATCH semantics at the
/// BFF: it reads the vessel's current detail and merges the provided fields over current values, so omitting a
/// field leaves it unchanged. Ownership is gated up front (the caller's default-page owned set) to yield a clean
/// not-found for a foreign/unknown id — never a 500 and never another owner's vessel. The freshness contract from
/// M4b holds: the module invalidates the vessel + the default list page (0,20) — the exact key the mobile reads —
/// so the change shows in detail and list immediately, and this handler returns the re-read detail projection
/// (never a re-scoped list).
/// </summary>
public sealed class UpdateMobileVesselCommandHandler
    : AizenCommandHandler<UpdateMobileVesselCommand, MobileVesselDetailDto>
{
    private const string DefaultLengthUnitCode = "METER";
    private const string DefaultEngineName = "Main Engine";

    // The module default page the write path invalidates — read the owned set here so a just-updated vessel
    // stays gate-visible (M4b lesson: reading a wider page would serve a stale cached page).
    private const int OwnedPageIndex = 0;
    private const int OwnedPageSize = 20;

    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;
    private readonly IReferenceDataRemoteCall _referenceData;
    private readonly ILogger<UpdateMobileVesselCommandHandler> _logger;

    public UpdateMobileVesselCommandHandler(
        IParticipantProfileResolver resolver,
        IVesselRemoteCall vessel,
        IReferenceDataRemoteCall referenceData,
        ILogger<UpdateMobileVesselCommandHandler> logger)
    {
        _referenceData = referenceData;
        _resolver = resolver;
        _vessel = vessel;
        _logger = logger;
    }

    public override async Task<MobileVesselDetailDto?> Handle(
        UpdateMobileVesselCommand request, CancellationToken cancellationToken)
    {
        var payload = request.Request ?? throw new AizenBusinessException("Vessel payload is required.");

        // Resolve → sets the identity holder so the owner-gated module writes assert as this participant.
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        // Ownership gate (clean not-found): only the caller's own vessels are editable. Read the default page —
        // the same key writes invalidate — so a vessel just created/edited by this caller is present.
        var ownedResp = await _vessel.GetUserVessels(OwnedPageIndex, OwnedPageSize);
        var owns = ownedResp?.Body?.Vessels?.Items?.Any(v => v.Id == request.VesselId) ?? false;
        if (!owns)
            throw new AizenBusinessException("Vessel not found.");

        // Current state — the patch base. Missing detail ⇒ treat as not-found rather than blindly overwriting.
        var current = (await _vessel.GetVesselDetail(request.VesselId))?.Body?.Vessel;
        if (current?.Vessel is null)
            throw new AizenBusinessException("Vessel not found.");

        // 1) Core (required, full controlled update merged over current values).
        var core = payload.Core;
        var cur = current.Vessel;
        var updateResp = await _vessel.UpdateVessel(request.VesselId, new UpdateVesselRequest
        {
            Name = FirstNonBlank(core?.Name, cur.Name),
            VesselTypeCode = FirstNonBlank(core?.VesselTypeCode, cur.VesselTypeCode),
            Description = NullIfBlank(core?.Description) ?? cur.Description,
            FlagCountryCode = NullIfBlank(core?.FlagCountryCode) ?? cur.FlagCountryCode,
            RegistrationNumber = NullIfBlank(core?.RegistrationNumber) ?? cur.RegistrationNumber,
            ImoNumber = NullIfBlank(core?.ImoNumber) ?? cur.ImoNumber,
            // Fields the mobile form does not edit — preserved from current so the full update never nulls them.
            VesselUsageTypeCode = cur.VesselUsageTypeCode,
            MmsiNumber = cur.MmsiNumber,
            CallSign = cur.CallSign,
            HomeCountryCode = cur.HomeCountryCode,
            HomeCityCode = cur.HomeCityCode,
            HomeDistrictCode = cur.HomeDistrictCode,
            HomeMarinaName = cur.HomeMarinaName,
        });
        if (updateResp?.Body?.Vessel is null)
            throw new AizenBusinessException("Vessel could not be updated.");

        // 2) Specification (best-effort; only when the client sent a spec object). Merged over current spec.
        if (payload.Spec is not null)
        {
            var s = payload.Spec;
            var cs = current.Specification;
            var length = s.LengthMeters ?? cs?.LengthValue;
            var beam = s.BeamMeters ?? cs?.BeamValue;
            var draft = s.DraftMeters ?? cs?.DraftValue;
            var (uvBrand, uvModel, uvBrandId, uvModelId) = await MobileCatalogDenormalizer.ResolveVesselAsync(
                _referenceData, s.VesselModelId, s.VesselBrandId, s.Brand, s.Model);
            try
            {
                await _vessel.UpsertSpecification(request.VesselId, new UpsertVesselSpecificationRequest
                {
                    Brand = uvBrand ?? cs?.Brand,
                    Model = uvModel ?? cs?.Model,
                    VesselBrandId = uvBrandId ?? cs?.VesselBrandId,
                    VesselModelId = uvModelId ?? cs?.VesselModelId,
                    ProductionYear = s.ProductionYear ?? cs?.ProductionYear,
                    LengthValue = length,
                    LengthUnitCode = length.HasValue ? (cs?.LengthUnitCode ?? DefaultLengthUnitCode) : null,
                    BeamValue = beam,
                    BeamUnitCode = beam.HasValue ? (cs?.BeamUnitCode ?? DefaultLengthUnitCode) : null,
                    DraftValue = draft,
                    DraftUnitCode = draft.HasValue ? (cs?.DraftUnitCode ?? DefaultLengthUnitCode) : null,
                    CabinCount = s.CabinCount ?? cs?.CabinCount,
                    HullMaterialCode = NullIfBlank(s.HullMaterialCode) ?? cs?.HullMaterialCode,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Vessel {VesselId} updated but specification persist failed.", request.VesselId);
            }
        }

        // 3) Engine (best-effort; only when the client sent an engine object). Update the primary/existing engine
        // (merged over current), or add one if the vessel has none yet.
        if (payload.Engine is not null)
        {
            await ApplyEngineAsync(request.VesselId, payload.Engine, current.Engines, cancellationToken);
        }

        // Return the re-read detail (BuildDetail) so the client sees exactly what persisted — never a paged list.
        var detail = (await _vessel.GetVesselDetail(request.VesselId))?.Body?.Vessel;
        if (detail is not null)
            return MobileVesselMapper.MapDetail(detail);

        throw new AizenBusinessException("Vessel updated but detail could not be read.");
    }

    private async Task ApplyEngineAsync(
        long vesselId, UpdateMobileVesselEngineInput e, IReadOnlyList<VesselEngineDto> currentEngines, CancellationToken ct)
    {
        var existing = currentEngines?.FirstOrDefault(x => x.IsPrimary) ?? currentEngines?.FirstOrDefault();
        var (ceBrand, ceModel, ceHp, ceFuel, ceBrandId, ceModelId) = await MobileCatalogDenormalizer.ResolveEngineAsync(
            _referenceData, e.EngineModelId, e.EngineBrandId, e.HorsePower, e.FuelTypeCode);
        try
        {
            if (existing is not null)
            {
                await _vessel.UpdateEngine(vesselId, existing.Id, new UpdateVesselEngineRequest
                {
                    EngineName = FirstNonBlank(e.EngineName, existing.EngineName, DefaultEngineName),
                    EngineTypeCode = FirstNonBlank(e.EngineTypeCode, existing.EngineTypeCode),
                    FuelTypeCode = FirstNonBlank(ceFuel, e.FuelTypeCode, existing.FuelTypeCode),
                    HorsePower = ceHp ?? existing.HorsePower,
                    Brand = ceBrand ?? existing.Brand,
                    Model = ceModel ?? existing.Model,
                    EngineBrandId = ceBrandId ?? existing.EngineBrandId,
                    EngineModelId = ceModelId ?? existing.EngineModelId,
                    SerialNumber = existing.SerialNumber,
                    ProductionYear = existing.ProductionYear,
                });
            }
            else if (!string.IsNullOrWhiteSpace(e.EngineTypeCode) && !string.IsNullOrWhiteSpace(e.FuelTypeCode))
            {
                await _vessel.AddEngine(vesselId, new AddVesselEngineRequest
                {
                    EngineName = string.IsNullOrWhiteSpace(e.EngineName) ? DefaultEngineName : e.EngineName!.Trim(),
                    EngineTypeCode = e.EngineTypeCode,
                    FuelTypeCode = ceFuel ?? e.FuelTypeCode,
                    HorsePower = ceHp,
                    Brand = ceBrand,
                    Model = ceModel,
                    EngineBrandId = ceBrandId,
                    EngineModelId = ceModelId,
                    IsPrimary = true,
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Vessel {VesselId} updated but engine persist failed.", vesselId);
        }
    }

    private static string FirstNonBlank(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
