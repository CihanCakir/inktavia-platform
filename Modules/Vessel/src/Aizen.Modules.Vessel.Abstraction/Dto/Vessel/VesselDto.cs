using Aizen.Modules.Vessel.Abstraction.Enum;

namespace Aizen.Modules.Vessel.Abstraction.Dto.Vessel;

[DocumentationInfo("Vessel summary DTO", "Used for list views and basic vessel identification.")]
public sealed class VesselDto
{
    public long Id { get; set; }
    public Guid? PublicId { get; set; }
    public string VesselCode { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Slug { get; set; } = default!;
    public string? Description { get; set; }
    public string VesselTypeCode { get; set; } = default!;
    public string? VesselUsageTypeCode { get; set; }
    public string? FlagCountryCode { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? MmsiNumber { get; set; }
    public string? ImoNumber { get; set; }
    public string? CallSign { get; set; }
    public string? HomeCountryCode { get; set; }
    public string? HomeCityCode { get; set; }
    public string? HomeDistrictCode { get; set; }
    public string? HomeMarinaName { get; set; }
    public VesselStatus Status { get; set; }
    public VesselVisibility Visibility { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public VesselArchiveReason? ArchiveReason { get; set; }
    public DateTime? CreateDate { get; set; }
    public DateTime? ModifyDate { get; set; }
}
