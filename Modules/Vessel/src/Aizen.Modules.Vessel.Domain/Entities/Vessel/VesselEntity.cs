using Aizen.Core.Domain;
using Aizen.Modules.Vessel.Abstraction.Enum;

namespace Aizen.Modules.Vessel.Domain.Entities.Vessel;

[DocumentationInfo("Vessel entity", "Root aggregate representing a boat or yacht digital profile.")]
public sealed class VesselEntity : AizenEntityWithAudit
{
    public string VesselCode { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string Slug { get; private set; } = default!;
    public string? Description { get; private set; }
    public string VesselTypeCode { get; private set; } = default!;
    public string? VesselUsageTypeCode { get; private set; }
    public string? FlagCountryCode { get; private set; }
    public string? RegistrationNumber { get; private set; }
    public string? MmsiNumber { get; private set; }
    public string? ImoNumber { get; private set; }
    public string? CallSign { get; private set; }
    public string? HomeCountryCode { get; private set; }
    public string? HomeCityCode { get; private set; }
    public string? HomeDistrictCode { get; private set; }
    public string? HomeMarinaName { get; private set; }
    public VesselStatus Status { get; private set; }
    public VesselVisibility Visibility { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTime? ArchivedAt { get; private set; }
    public VesselArchiveReason? ArchiveReason { get; private set; }
    public int? OperationalStatus { get; private set; }
    public int? AssetType { get; private set; }

    private readonly List<VesselOwnerEntity> _owners = new();
    public IReadOnlyCollection<VesselOwnerEntity> Owners => _owners.AsReadOnly();

    public VesselSpecificationEntity? Specification { get; private set; }

    private readonly List<VesselEngineEntity> _engines = new();
    public IReadOnlyCollection<VesselEngineEntity> Engines => _engines.AsReadOnly();

    private readonly List<VesselDocumentEntity> _documents = new();
    public IReadOnlyCollection<VesselDocumentEntity> Documents => _documents.AsReadOnly();

    private readonly List<VesselMediaEntity> _media = new();
    public IReadOnlyCollection<VesselMediaEntity> Media => _media.AsReadOnly();

    private readonly List<VesselLocationSnapshotEntity> _locationSnapshots = new();
    public IReadOnlyCollection<VesselLocationSnapshotEntity> LocationSnapshots => _locationSnapshots.AsReadOnly();

    private readonly List<VesselStatusHistoryEntity> _statusHistory = new();
    public IReadOnlyCollection<VesselStatusHistoryEntity> StatusHistory => _statusHistory.AsReadOnly();

    public VesselEntity() { }

    public static VesselEntity Create(
        string vesselCode,
        string name,
        string slug,
        string vesselTypeCode,
        string? description,
        string? vesselUsageTypeCode,
        string? flagCountryCode,
        string? registrationNumber,
        string? mmsiNumber,
        string? imoNumber,
        string? callSign,
        string? homeCountryCode,
        string? homeCityCode,
        string? homeDistrictCode,
        string? homeMarinaName,
        VesselVisibility visibility)
    {
        return new VesselEntity
        {
            VesselCode = vesselCode.ToUpperInvariant(),
            Name = name.Trim(),
            Slug = slug.ToLowerInvariant().Trim(),
            Description = description,
            VesselTypeCode = vesselTypeCode.ToUpperInvariant(),
            VesselUsageTypeCode = vesselUsageTypeCode?.ToUpperInvariant(),
            FlagCountryCode = flagCountryCode?.ToUpperInvariant(),
            RegistrationNumber = registrationNumber,
            MmsiNumber = mmsiNumber,
            ImoNumber = imoNumber,
            CallSign = callSign,
            HomeCountryCode = homeCountryCode?.ToUpperInvariant(),
            HomeCityCode = homeCityCode?.ToUpperInvariant(),
            HomeDistrictCode = homeDistrictCode?.ToUpperInvariant(),
            HomeMarinaName = homeMarinaName,
            Status = VesselStatus.Draft,
            Visibility = visibility,
            IsArchived = false,
            IsActive = true
        };
    }

    public void UpdateProfile(
        string name,
        string? description,
        string vesselTypeCode,
        string? vesselUsageTypeCode,
        string? flagCountryCode,
        string? registrationNumber,
        string? mmsiNumber,
        string? imoNumber,
        string? callSign,
        string? homeCountryCode,
        string? homeCityCode,
        string? homeDistrictCode,
        string? homeMarinaName)
    {
        Name = name.Trim();
        Description = description;
        VesselTypeCode = vesselTypeCode.ToUpperInvariant();
        VesselUsageTypeCode = vesselUsageTypeCode?.ToUpperInvariant();
        FlagCountryCode = flagCountryCode?.ToUpperInvariant();
        RegistrationNumber = registrationNumber;
        MmsiNumber = mmsiNumber;
        ImoNumber = imoNumber;
        CallSign = callSign;
        HomeCountryCode = homeCountryCode?.ToUpperInvariant();
        HomeCityCode = homeCityCode?.ToUpperInvariant();
        HomeDistrictCode = homeDistrictCode?.ToUpperInvariant();
        HomeMarinaName = homeMarinaName;
    }

    public void ChangeStatus(VesselStatus newStatus) => Status = newStatus;

    public void UpdateVisibility(VesselVisibility visibility) => Visibility = visibility;

    public void Archive(VesselArchiveReason reason)
    {
        IsArchived = true;
        ArchivedAt = DateTime.UtcNow;
        ArchiveReason = reason;
        Status = VesselStatus.Archived;
        IsActive = false;
    }

    public void Restore()
    {
        IsArchived = false;
        ArchivedAt = null;
        ArchiveReason = null;
        Status = VesselStatus.Passive;
        IsActive = true;
    }

    public void AddStatusHistory(VesselStatusHistoryEntity entry) => _statusHistory.Add(entry);

    public void UpdateOperationalStatus(int? operationalStatus) => OperationalStatus = operationalStatus;
    public void UpdateAssetType(int? assetType) => AssetType = assetType;
}
