using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Model;

namespace Aizen.Modules.Vessel.Abstraction.Request.Vessel;

[DocumentationInfo("Create vessel request", "Input model for creating a new vessel profile.")]
public sealed class CreateVesselRequest
{
    public string Name { get; set; } = default!;
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
    public VesselVisibility Visibility { get; set; } = VesselVisibility.Private;
    public long OwnerUserId { get; set; }
}
