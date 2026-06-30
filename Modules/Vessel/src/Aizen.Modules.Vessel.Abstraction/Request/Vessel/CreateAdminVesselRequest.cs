using Aizen.Modules.Vessel.Abstraction.Enum;

namespace Aizen.Modules.Vessel.Abstraction.Request.Vessel;

[DocumentationInfo("Create admin vessel request", "Input model for admin-initiated vessel creation. Allows specifying an explicit owner user ID instead of using the caller identity.")]
public sealed class CreateAdminVesselRequest
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

    /// <summary>
    /// The user ID of the vessel's primary owner. Admin may assign ownership explicitly.
    /// </summary>
    public long OwnerUserId { get; set; }

    /// <summary>
    /// Optional profile ID of the primary owner.
    /// </summary>
    public long? OwnerProfileId { get; set; }
}
