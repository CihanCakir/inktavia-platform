using Aizen.Modules.Vessel.Abstraction.Dto.Document;
using Aizen.Modules.Vessel.Abstraction.Dto.Engine;
using Aizen.Modules.Vessel.Abstraction.Dto.Location;
using Aizen.Modules.Vessel.Abstraction.Dto.Media;
using Aizen.Modules.Vessel.Abstraction.Dto.Ownership;
using Aizen.Modules.Vessel.Abstraction.Dto.Specification;
using Aizen.Modules.Vessel.Abstraction.Dto.Status;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Domain.Documents;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Aizen.Modules.Vessel.Domain.Interface.Repository;
using Aizen.Modules.Vessel.Domain.Interface.Service;
using Aizen.Modules.Vessel.Repository.Mongo;

namespace Aizen.Modules.Vessel.Repository.Service;

[DocumentationInfo("Vessel snapshot service", "Builds denormalized VesselDetailDto and maintains the MongoDB read-side snapshot.")]
public sealed class VesselSnapshotService : IVesselSnapshotService
{
    private readonly IVesselRepository _vesselRepository;
    private readonly VesselProfileReadRepository _readRepository;

    public VesselSnapshotService(IVesselRepository vesselRepository, VesselProfileReadRepository readRepository)
    {
        _vesselRepository = vesselRepository;
        _readRepository = readRepository;
    }

    public async Task<VesselDetailDto?> BuildDetailAsync(long vesselId, CancellationToken ct = default)
    {
        var vessel = await _vesselRepository.GetByIdWithDetailsAsync(vesselId, ct);
        if (vessel is null) return null;

        // Full detail: the aggregate is eager-loaded (GetByIdWithDetailsAsync includes spec/engines/docs/media/
        // location/status/owners) — project ALL of it, not just the core vessel. (Previously only Vessel was
        // mapped, so detail always returned null spec/engines/etc.)
        return new VesselDetailDto
        {
            Vessel = MapVessel(vessel),
            Owners = vessel.Owners.Select(MapOwner).ToList(),
            Specification = vessel.Specification is null ? null : MapSpecification(vessel.Specification),
            Engines = vessel.Engines.Select(MapEngine).ToList(),
            Documents = vessel.Documents.Select(MapDocument).ToList(),
            Media = vessel.Media.Select(MapMedia).ToList(),
            CurrentLocation = vessel.LocationSnapshots.FirstOrDefault(l => l.IsCurrent) is { } loc ? MapLocation(loc) : null,
            StatusHistory = vessel.StatusHistory.Select(MapStatusHistory).ToList()
        };
    }

    private static VesselDto MapVessel(VesselEntity vessel) => new()
    {
        Id = vessel.Id,
        PublicId = vessel.PublicId,
        VesselCode = vessel.VesselCode,
        Name = vessel.Name,
        Slug = vessel.Slug,
        Description = vessel.Description,
        VesselTypeCode = vessel.VesselTypeCode,
        VesselUsageTypeCode = vessel.VesselUsageTypeCode,
        FlagCountryCode = vessel.FlagCountryCode,
        RegistrationNumber = vessel.RegistrationNumber,
        MmsiNumber = vessel.MmsiNumber,
        ImoNumber = vessel.ImoNumber,
        CallSign = vessel.CallSign,
        HomeCountryCode = vessel.HomeCountryCode,
        HomeCityCode = vessel.HomeCityCode,
        HomeDistrictCode = vessel.HomeDistrictCode,
        HomeMarinaName = vessel.HomeMarinaName,
        Status = vessel.Status,
        Visibility = vessel.Visibility,
        IsArchived = vessel.IsArchived,
        ArchiveReason = vessel.ArchiveReason,
        ArchivedAt = vessel.ArchivedAt,
        CreateDate = vessel.CreateDate,
        ModifyDate = vessel.ModifyDate
    };

    private static VesselOwnerDto MapOwner(VesselOwnerEntity o) => new()
    {
        Id = o.Id,
        VesselId = o.VesselId,
        UserId = o.UserId,
        UserProfileId = o.UserProfileId,
        Role = o.Role,
        Status = o.OwnershipStatus,
        IsPrimary = o.IsPrimary,
        InvitedAt = o.InvitedAt,
        AcceptedAt = o.AcceptedAt,
        RemovedAt = o.RemovedAt
    };

    private static VesselSpecificationDto MapSpecification(VesselSpecificationEntity s) => new()
    {
        Id = s.Id,
        VesselId = s.VesselId,
        Brand = s.Brand,
        Model = s.Model,
        ProductionYear = s.ProductionYear,
        LengthValue = s.LengthValue,
        LengthUnitCode = s.LengthUnitCode,
        BeamValue = s.BeamValue,
        BeamUnitCode = s.BeamUnitCode,
        DraftValue = s.DraftValue,
        DraftUnitCode = s.DraftUnitCode,
        WeightValue = s.WeightValue,
        WeightUnitCode = s.WeightUnitCode,
        CabinCount = s.CabinCount,
        BedCount = s.BedCount,
        BathroomCount = s.BathroomCount,
        HullMaterialCode = s.HullMaterialCode,
        FuelCapacityValue = s.FuelCapacityValue,
        FuelCapacityUnitCode = s.FuelCapacityUnitCode,
        WaterCapacityValue = s.WaterCapacityValue,
        WaterCapacityUnitCode = s.WaterCapacityUnitCode,
        BuildCountry = s.BuildCountry,
        SuperstructureMaterial = s.SuperstructureMaterial,
        GrossTonnage = s.GrossTonnage,
        NetTonnage = s.NetTonnage,
        PassengerCapacity = s.PassengerCapacity,
        CrewCapacity = s.CrewCapacity
    };

    private static VesselEngineDto MapEngine(VesselEngineEntity e) => new()
    {
        Id = e.Id,
        VesselId = e.VesselId,
        EngineName = e.EngineName,
        EngineTypeCode = e.EngineTypeCode,
        FuelTypeCode = e.FuelTypeCode,
        Brand = e.Brand,
        Model = e.Model,
        SerialNumber = e.SerialNumber,
        HorsePower = e.HorsePower,
        ProductionYear = e.ProductionYear,
        IsPrimary = e.IsPrimary,
        IsActive = e.IsActive,
        PropulsionType = e.PropulsionType,
        EnginePowerKw = e.EnginePowerKw,
        FuelCapacityL = e.FuelCapacityL,
        MaxSpeedKnots = e.MaxSpeedKnots,
        CruisingSpeedKnots = e.CruisingSpeedKnots,
        RangeNm = e.RangeNm
    };

    private static VesselDocumentDto MapDocument(VesselDocumentEntity d) => new()
    {
        Id = d.Id,
        VesselId = d.VesselId,
        DocumentTypeCode = d.DocumentTypeCode,
        DocumentName = d.DocumentName,
        FileId = d.FileId,
        OriginalFileNameSnapshot = d.OriginalFileNameSnapshot,
        ContentTypeSnapshot = d.ContentTypeSnapshot,
        SizeInBytesSnapshot = d.SizeInBytesSnapshot,
        ExpiresAt = d.ExpiresAt,
        Status = d.DocumentStatus,
        Notes = d.Notes,
        IsActive = d.IsActive,
        DocumentCategory = d.DocumentCategory,
        IssuingAuthority = d.IssuingAuthority,
        ApprovedAt = d.ApprovedAt,
        ApprovedByUserId = d.ApprovedByUserId
    };

    private static VesselMediaDto MapMedia(VesselMediaEntity m) => new()
    {
        Id = m.Id,
        VesselId = m.VesselId,
        MediaType = m.MediaType,
        FileId = m.FileId,
        OriginalFileNameSnapshot = m.OriginalFileNameSnapshot,
        ContentTypeSnapshot = m.ContentTypeSnapshot,
        SizeInBytesSnapshot = m.SizeInBytesSnapshot,
        SortOrder = m.SortOrder,
        IsCover = m.IsCover,
        IsActive = m.IsActive,
        Title = m.Title,
        Description = m.Description,
        ThumbnailUrl = m.ThumbnailUrl,
        UploadedByUserId = m.UploadedByUserId
    };

    private static VesselLocationSnapshotDto MapLocation(VesselLocationSnapshotEntity l) => new()
    {
        Id = l.Id,
        VesselId = l.VesselId,
        CountryCode = l.CountryCode,
        CityCode = l.CityCode,
        DistrictCode = l.DistrictCode,
        MarinaName = l.MarinaName,
        Latitude = l.Latitude,
        Longitude = l.Longitude,
        AccuracyMeters = l.AccuracyMeters,
        Source = l.Source,
        CapturedAt = l.CapturedAt,
        IsCurrent = l.IsCurrent
    };

    private static VesselStatusHistoryDto MapStatusHistory(VesselStatusHistoryEntity s) => new()
    {
        Id = s.Id,
        VesselId = s.VesselId,
        FromStatus = s.FromStatus,
        ToStatus = s.ToStatus,
        Reason = s.Reason,
        ChangedByUserId = s.ChangedByUserId,
        ChangedAt = s.ChangedAt
    };

    public async Task SyncReadDocumentAsync(long vesselId, CancellationToken ct = default)
    {
        var vessel = await _vesselRepository.GetByIdWithDetailsAsync(vesselId, ct);
        if (vessel is null) return;

        var doc = new VesselProfileReadDocument
        {
            VesselId = vessel.Id,
            PublicId = vessel.PublicId,
            VesselCode = vessel.VesselCode,
            Name = vessel.Name,
            Slug = vessel.Slug,
            VesselTypeCode = vessel.VesselTypeCode,
            FlagCountryCode = vessel.FlagCountryCode,
            Status = vessel.Status,
            Visibility = vessel.Visibility,
            IsArchived = vessel.IsArchived,
            OwnerUserIds = vessel.Owners.Where(o => o.IsActive).Select(o => o.UserId).ToList(),
            CoverMediaUrl = null, // AccessUrl is dynamically generated, not persisted
            UpdatedAt = DateTime.UtcNow,
            IsDeleted = vessel.IsDeleted,
        };

        await _readRepository.UpsertAsync(doc, ct);
    }
}
