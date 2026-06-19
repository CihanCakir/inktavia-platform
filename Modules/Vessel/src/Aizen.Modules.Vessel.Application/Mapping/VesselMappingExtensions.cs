using Aizen.Modules.Vessel.Abstraction.Dto.Document;
using Aizen.Modules.Vessel.Abstraction.Dto.Engine;
using Aizen.Modules.Vessel.Abstraction.Dto.Location;
using Aizen.Modules.Vessel.Abstraction.Dto.Media;
using Aizen.Modules.Vessel.Abstraction.Dto.Ownership;
using Aizen.Modules.Vessel.Abstraction.Dto.Specification;
using Aizen.Modules.Vessel.Abstraction.Dto.Status;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;
using Aizen.Modules.Vessel.Domain.Entities.Vessel;

namespace Aizen.Modules.Vessel.Application.Mapping;

public static class VesselMappingExtensions
{
    public static VesselDto ToDto(this VesselEntity entity) => new()
    {
        Id = entity.Id,
        PublicId = entity.PublicId,
        VesselCode = entity.VesselCode,
        Name = entity.Name,
        Slug = entity.Slug,
        Description = entity.Description,
        VesselTypeCode = entity.VesselTypeCode,
        VesselUsageTypeCode = entity.VesselUsageTypeCode,
        FlagCountryCode = entity.FlagCountryCode,
        RegistrationNumber = entity.RegistrationNumber,
        MmsiNumber = entity.MmsiNumber,
        ImoNumber = entity.ImoNumber,
        CallSign = entity.CallSign,
        HomeCountryCode = entity.HomeCountryCode,
        HomeCityCode = entity.HomeCityCode,
        HomeDistrictCode = entity.HomeDistrictCode,
        HomeMarinaName = entity.HomeMarinaName,
        Status = entity.Status,
        Visibility = entity.Visibility,
        IsArchived = entity.IsArchived,
        ArchivedAt = entity.ArchivedAt,
        ArchiveReason = entity.ArchiveReason,
        CreateDate = entity.CreateDate,
        ModifyDate = entity.ModifyDate
    };

    public static VesselDetailDto ToDetailDto(this VesselEntity entity) => new()
    {
        Vessel = entity.ToDto(),
        Owners = entity.Owners.Select(o => o.ToDto()).ToList(),
        Specification = entity.Specification?.ToDto(),
        Engines = entity.Engines.Select(e => e.ToDto()).ToList(),
        Documents = entity.Documents.Select(d => d.ToDto()).ToList(),
        Media = entity.Media.Select(m => m.ToDto()).ToList(),
        CurrentLocation = entity.LocationSnapshots.FirstOrDefault(l => l.IsCurrent)?.ToDto(),
        StatusHistory = entity.StatusHistory.Select(s => s.ToDto()).ToList()
    };

    public static VesselListItemDto ToListItemDto(this VesselEntity entity) => new()
    {
        Id = entity.Id,
        PublicId = entity.PublicId,
        VesselCode = entity.VesselCode,
        Name = entity.Name,
        Slug = entity.Slug,
        VesselTypeCode = entity.VesselTypeCode,
        FlagCountryCode = entity.FlagCountryCode,
        CoverMediaUrl = null, // AccessUrl is dynamically generated, not persisted
        Status = entity.Status,
        Visibility = entity.Visibility,
        IsArchived = entity.IsArchived,
        CreateDate = entity.CreateDate,
        OperationalStatus = entity.OperationalStatus,
        AssetType = entity.AssetType,
        OwnershipStatus = null, // requires join, populated by query projection
        OwnerName = null, // requires identity join, populated by BFF
        LengthMeters = entity.Specification?.LengthValue,
        GrossTonnage = entity.Specification?.GrossTonnage,
        Latitude = entity.LocationSnapshots?.FirstOrDefault(l => l.IsCurrent)?.Latitude != null ? (double?)decimal.ToDouble(entity.LocationSnapshots!.First(l => l.IsCurrent).Latitude!.Value) : null,
        Longitude = entity.LocationSnapshots?.FirstOrDefault(l => l.IsCurrent)?.Longitude != null ? (double?)decimal.ToDouble(entity.LocationSnapshots!.First(l => l.IsCurrent).Longitude!.Value) : null,
        LastPositionDate = entity.LocationSnapshots?.FirstOrDefault(l => l.IsCurrent)?.CapturedAt
    };

    public static VesselOwnerDto ToDto(this VesselOwnerEntity entity) => new()
    {
        Id = entity.Id,
        VesselId = entity.VesselId,
        UserId = entity.UserId,
        UserProfileId = entity.UserProfileId,
        Role = entity.Role,
        Status = entity.OwnershipStatus,
        IsPrimary = entity.IsPrimary,
        InvitedAt = entity.InvitedAt,
        AcceptedAt = entity.AcceptedAt,
        RemovedAt = entity.RemovedAt
    };

    public static VesselSpecificationDto ToDto(this VesselSpecificationEntity entity) => new()
    {
        Id = entity.Id,
        VesselId = entity.VesselId,
        Brand = entity.Brand,
        Model = entity.Model,
        ProductionYear = entity.ProductionYear,
        LengthValue = entity.LengthValue,
        LengthUnitCode = entity.LengthUnitCode,
        BeamValue = entity.BeamValue,
        BeamUnitCode = entity.BeamUnitCode,
        DraftValue = entity.DraftValue,
        DraftUnitCode = entity.DraftUnitCode,
        WeightValue = entity.WeightValue,
        WeightUnitCode = entity.WeightUnitCode,
        CabinCount = entity.CabinCount,
        BedCount = entity.BedCount,
        BathroomCount = entity.BathroomCount,
        HullMaterialCode = entity.HullMaterialCode,
        FuelCapacityValue = entity.FuelCapacityValue,
        FuelCapacityUnitCode = entity.FuelCapacityUnitCode,
        WaterCapacityValue = entity.WaterCapacityValue,
        WaterCapacityUnitCode = entity.WaterCapacityUnitCode,
        BuildCountry = entity.BuildCountry,
        SuperstructureMaterial = entity.SuperstructureMaterial,
        GrossTonnage = entity.GrossTonnage,
        NetTonnage = entity.NetTonnage,
        PassengerCapacity = entity.PassengerCapacity,
        CrewCapacity = entity.CrewCapacity
    };

    public static VesselEngineDto ToDto(this VesselEngineEntity entity) => new()
    {
        Id = entity.Id,
        VesselId = entity.VesselId,
        EngineName = entity.EngineName,
        EngineTypeCode = entity.EngineTypeCode,
        FuelTypeCode = entity.FuelTypeCode,
        Brand = entity.Brand,
        Model = entity.Model,
        SerialNumber = entity.SerialNumber,
        HorsePower = entity.HorsePower,
        ProductionYear = entity.ProductionYear,
        IsPrimary = entity.IsPrimary,
        IsActive = entity.IsActive,
        PropulsionType = entity.PropulsionType,
        EnginePowerKw = entity.EnginePowerKw,
        FuelCapacityL = entity.FuelCapacityL,
        MaxSpeedKnots = entity.MaxSpeedKnots,
        CruisingSpeedKnots = entity.CruisingSpeedKnots,
        RangeNm = entity.RangeNm
    };

    public static VesselDocumentDto ToDto(this VesselDocumentEntity entity) => new()
    {
        Id = entity.Id,
        VesselId = entity.VesselId,
        DocumentTypeCode = entity.DocumentTypeCode,
        DocumentName = entity.DocumentName,
        FileId = entity.FileId,
        OriginalFileNameSnapshot = entity.OriginalFileNameSnapshot,
        ContentTypeSnapshot = entity.ContentTypeSnapshot,
        SizeInBytesSnapshot = entity.SizeInBytesSnapshot,
        ExpiresAt = entity.ExpiresAt,
        Status = entity.DocumentStatus,
        Notes = entity.Notes,
        IsActive = entity.IsActive,
        DocumentCategory = entity.DocumentCategory,
        IssuingAuthority = entity.IssuingAuthority,
        ApprovedAt = entity.ApprovedAt,
        ApprovedByUserId = entity.ApprovedByUserId
    };

    public static VesselMediaDto ToDto(this VesselMediaEntity entity) => new()
    {
        Id = entity.Id,
        VesselId = entity.VesselId,
        MediaType = entity.MediaType,
        FileId = entity.FileId,
        OriginalFileNameSnapshot = entity.OriginalFileNameSnapshot,
        ContentTypeSnapshot = entity.ContentTypeSnapshot,
        SizeInBytesSnapshot = entity.SizeInBytesSnapshot,
        SortOrder = entity.SortOrder,
        IsCover = entity.IsCover,
        IsActive = entity.IsActive,
        Title = entity.Title,
        Description = entity.Description,
        ThumbnailUrl = entity.ThumbnailUrl,
        UploadedByUserId = entity.UploadedByUserId
    };

    public static VesselLocationSnapshotDto ToDto(this VesselLocationSnapshotEntity entity) => new()
    {
        Id = entity.Id,
        VesselId = entity.VesselId,
        CountryCode = entity.CountryCode,
        CityCode = entity.CityCode,
        DistrictCode = entity.DistrictCode,
        MarinaName = entity.MarinaName,
        Latitude = entity.Latitude,
        Longitude = entity.Longitude,
        AccuracyMeters = entity.AccuracyMeters,
        Source = entity.Source,
        CapturedAt = entity.CapturedAt,
        IsCurrent = entity.IsCurrent
    };

    public static VesselStatusHistoryDto ToDto(this VesselStatusHistoryEntity entity) => new()
    {
        Id = entity.Id,
        VesselId = entity.VesselId,
        FromStatus = entity.FromStatus,
        ToStatus = entity.ToStatus,
        Reason = entity.Reason,
        ChangedByUserId = entity.ChangedByUserId,
        ChangedAt = entity.ChangedAt
    };
}
