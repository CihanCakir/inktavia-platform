using Aizen.Core.CQRS.Message;
using Aizen.Modules.Vessel.Abstraction.Enum;
using Aizen.Modules.Vessel.Abstraction.Response.Vessel;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Command;

/// <summary>Input model for the Admin Panel Vessel Register form submission.</summary>
public sealed class RegisterAdminVesselBffRequest
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

    /// <summary>ID of the user who will be set as primary owner. Must be long.</summary>
    public long OwnerUserId { get; set; }

    /// <summary>Optional profile ID of the primary owner.</summary>
    public long? OwnerProfileId { get; set; }
}

/// <summary>Carries the register form payload from the AdminPanel BFF to the Vessel module admin create endpoint.</summary>
public sealed class RegisterAdminVesselBffCommand : AizenCommand<CreateVesselResponse>
{
    public RegisterAdminVesselBffRequest Payload { get; }
    public string UserToken { get; }

    public RegisterAdminVesselBffCommand(RegisterAdminVesselBffRequest payload, string userToken)
    {
        Payload = payload;
        UserToken = userToken;
    }
}
