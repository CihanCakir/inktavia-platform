using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Common.Services;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Request.Engine;
using Aizen.Modules.Vessel.Abstraction.Request.Specification;
using Aizen.Modules.Vessel.Abstraction.Request.Vessel;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>
/// Create orchestration (M4b). The module has no cross-call transaction, so this composes three module writes:
///   1) POST /vessels          — REQUIRED. Creates the core and assigns the asserted caller as PrimaryOwner.
///   2) PUT  /vessels/{id}/specification — optional enrichment (only when the wizard sent any spec field).
///   3) POST /vessels/{id}/engines       — optional enrichment (only when an engine type+fuel were chosen).
/// Step 1 is authoritative: if it fails the whole request fails (no half-create — nothing was persisted yet).
/// Steps 2–3 are best-effort — a failure is logged (not swallowed silently) and the returned detail reflects
/// exactly what persisted, so the client never sees a success that hides a dropped spec/engine. The identity
/// holder set by <see cref="IParticipantProfileResolver"/> asserts every downstream call as this participant,
/// which is why the owner-gated spec/engine writes are authorized against the just-created vessel.
/// </summary>
public sealed class CreateMobileVesselCommandHandler
    : AizenCommandHandler<CreateMobileVesselCommand, MobileVesselDetailDto>
{
    private const string DefaultLengthUnitCode = "METER";
    private const string DefaultEngineName = "Main Engine";

    private readonly IParticipantProfileResolver _resolver;
    private readonly IVesselRemoteCall _vessel;
    private readonly IReferenceDataRemoteCall _referenceData;
    private readonly ILogger<CreateMobileVesselCommandHandler> _logger;

    public CreateMobileVesselCommandHandler(
        IParticipantProfileResolver resolver,
        IVesselRemoteCall vessel,
        IReferenceDataRemoteCall referenceData,
        ILogger<CreateMobileVesselCommandHandler> logger)
    {
        _referenceData = referenceData;
        _resolver = resolver;
        _vessel = vessel;
        _logger = logger;
    }

    public override async Task<MobileVesselDetailDto?> Handle(
        CreateMobileVesselCommand request, CancellationToken cancellationToken)
    {
        var payload = request.Request ?? throw new AizenBusinessException("Vessel payload is required.");
        var core = payload.Core ?? throw new AizenBusinessException("Vessel details are required.");

        if (string.IsNullOrWhiteSpace(core.Name))
            throw new AizenBusinessException("Vessel name is required.");
        if (string.IsNullOrWhiteSpace(core.VesselTypeCode))
            throw new AizenBusinessException("Vessel type is required.");

        // Resolve → sets the identity holder so the create (and the owner-gated spec/engine writes) assert as this participant.
        var resolution = await _resolver.ResolveAsync(cancellationToken);
        if (resolution.ProfileId is not > 0)
            throw new AizenBusinessException("No participant profile is linked to this account yet.");

        // 1) Create the core (required). OwnerUserId is ignored module-side — the asserted caller becomes PrimaryOwner.
        var createResp = await _vessel.CreateVessel(new CreateVesselRequest
        {
            Name = core.Name.Trim(),
            VesselTypeCode = core.VesselTypeCode,
            FlagCountryCode = NullIfBlank(core.FlagCountryCode),
            RegistrationNumber = NullIfBlank(core.RegistrationNumber),
            Description = NullIfBlank(core.Description),
            ImoNumber = NullIfBlank(core.ImoNumber),
            Visibility = VesselVisibility.Private,
        });

        var created = createResp?.Body?.Vessel;
        if (created is null)
            throw new AizenBusinessException("Vessel could not be created.");

        // The module builds its response DTO from the entity BEFORE the unit-of-work commits, so the returned
        // Id is 0 (the DB identity is assigned at commit). The row IS committed by the time this returns, so
        // recover the real Id deterministically by the just-created vessel's globally-unique VesselCode — a
        // brand-new code is never cached, so the by-code read hits the DB and returns the persisted Id. This
        // replaces the previous re-read of the caller's paged list, which could serve a stale cached page
        // (the list read uses a different page size than the invalidation evicts) → a false "could not create".
        var vesselId = created.Id;
        if (vesselId <= 0 && !string.IsNullOrWhiteSpace(created.VesselCode))
        {
            var byCode = await _vessel.GetVesselByCode(created.VesselCode);
            vesselId = byCode?.Body?.Vessel?.Id ?? 0;
        }
        if (vesselId <= 0)
            throw new AizenBusinessException("Vessel could not be created.");

        // 2) Specification (optional). Only when the wizard actually captured a technical field.
        if (HasSpec(payload.Spec))
        {
            var s = payload.Spec!;
            var (vBrand, vModel, vBrandId, vModelId) = await MobileCatalogDenormalizer.ResolveVesselAsync(
                _referenceData, s.VesselModelId, s.VesselBrandId, s.Brand, s.Model);
            try
            {
                await _vessel.UpsertSpecification(vesselId, new UpsertVesselSpecificationRequest
                {
                    Brand = vBrand,
                    Model = vModel,
                    VesselBrandId = vBrandId,
                    VesselModelId = vModelId,
                    ProductionYear = s.ProductionYear,
                    LengthValue = s.LengthMeters,
                    LengthUnitCode = s.LengthMeters.HasValue ? DefaultLengthUnitCode : null,
                    BeamValue = s.BeamMeters,
                    BeamUnitCode = s.BeamMeters.HasValue ? DefaultLengthUnitCode : null,
                    DraftValue = s.DraftMeters,
                    DraftUnitCode = s.DraftMeters.HasValue ? DefaultLengthUnitCode : null,
                    CabinCount = s.CabinCount,
                    HullMaterialCode = NullIfBlank(s.HullMaterialCode),
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Vessel {VesselId} created but specification persist failed.", vesselId);
            }
        }

        // 3) Engine (optional). The wizard collects one configuration; engine count is informational only.
        if (HasEngine(payload.Engine))
        {
            var e = payload.Engine!;
            var (eBrand, eModel, eHp, eFuel, eBrandId, eModelId) = await MobileCatalogDenormalizer.ResolveEngineAsync(
                _referenceData, e.EngineModelId, e.EngineBrandId, e.HorsePower, e.FuelTypeCode);
            try
            {
                await _vessel.AddEngine(vesselId, new AddVesselEngineRequest
                {
                    EngineName = string.IsNullOrWhiteSpace(e.EngineName) ? DefaultEngineName : e.EngineName!.Trim(),
                    EngineTypeCode = e.EngineTypeCode,
                    FuelTypeCode = eFuel ?? e.FuelTypeCode,
                    HorsePower = eHp,
                    Brand = eBrand,
                    Model = eModel,
                    EngineBrandId = eBrandId,
                    EngineModelId = eModelId,
                    IsPrimary = true,
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Vessel {VesselId} created but engine persist failed.", vesselId);
            }
        }

        // Return the assembled detail so the client sees exactly what persisted (reuses the M4a projection).
        var detailResp = await _vessel.GetVesselDetail(vesselId);
        var detail = detailResp?.Body?.Vessel;
        if (detail is not null)
            return MobileVesselMapper.MapDetail(detail);

        // Detail read failed post-create — echo a minimal projection from the create response rather than 500.
        return new MobileVesselDetailDto
        {
            Id = created.Id,
            Name = created.Name,
            TypeCode = created.VesselTypeCode,
            Flag = created.FlagCountryCode,
            Status = created.Status.ToString(),
            Description = created.Description,
            RegistrationNumber = created.RegistrationNumber,
            ImoNumber = created.ImoNumber,
        };
    }

    private static bool HasSpec(MobileVesselSpecInput? s) =>
        s is not null && (s.ProductionYear.HasValue || s.LengthMeters.HasValue || s.BeamMeters.HasValue ||
                          s.DraftMeters.HasValue || s.GrossTonnage.HasValue || s.CabinCount.HasValue ||
                          !string.IsNullOrWhiteSpace(s.HullMaterialCode) || !string.IsNullOrWhiteSpace(s.Brand) ||
                          !string.IsNullOrWhiteSpace(s.Model));

    private static bool HasEngine(MobileVesselEngineInput? e) =>
        e is not null && !string.IsNullOrWhiteSpace(e.EngineTypeCode) && !string.IsNullOrWhiteSpace(e.FuelTypeCode);

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
