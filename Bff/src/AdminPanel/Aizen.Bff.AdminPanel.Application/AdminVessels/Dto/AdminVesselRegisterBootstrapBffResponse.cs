using Aizen.Bff.AdminPanel.Application.Common;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;

[DocumentationInfo("Admin vessel register bootstrap BFF response", "Bootstrap options and defaults for the Vessel Register page.")]
public sealed class AdminVesselRegisterBootstrapBffResponse
{
    public VesselRegisterBootstrapBffDto? VesselRegister { get; set; }
    public List<AdminBffWarning> Warnings { get; set; } = new();
}

[DocumentationInfo("Vessel register bootstrap BFF DTO", "Defaults and option lists for the Vessel Register form.")]
public sealed class VesselRegisterBootstrapBffDto
{
    public VesselRegisterDefaultsBffDto Defaults { get; set; } = new();
    public VesselRegisterOptionsBffDto Options { get; set; } = new();
}

[DocumentationInfo("Vessel register defaults BFF DTO", "Pre-filled default values for the Vessel Register form fields.")]
public sealed class VesselRegisterDefaultsBffDto
{
    public string FlagCountryCode { get; set; } = "TR";
    public int AssetType { get; set; } = 1;
    public int OperationalStatus { get; set; } = 1;
    public int OwnershipStatus { get; set; } = 1;
    public bool IsArchived { get; set; } = false;
}

[DocumentationInfo("Vessel register options BFF DTO", "Dropdown option lists for the Vessel Register form.")]
public sealed class VesselRegisterOptionsBffDto
{
    public List<CodeLabelBffDto> VesselTypes { get; set; } = new();
    public List<ValueLabelBffDto> AssetTypes { get; set; } = new();
    public List<ValueLabelBffDto> OperationalStatuses { get; set; } = new();
    public List<ValueLabelBffDto> OwnershipStatuses { get; set; } = new();
    public List<CodeLabelBffDto> FlagCountries { get; set; } = new();
    public List<CodeLabelBffDto> BuildCountries { get; set; } = new();
    public List<CodeLabelBffDto> HullMaterials { get; set; } = new();
    public List<CodeLabelBffDto> SuperstructureMaterials { get; set; } = new();
    public List<CodeLabelBffDto> HomePorts { get; set; } = new();
    public List<OwnerCandidateBffDto> OwnerCandidates { get; set; } = new();
}

[DocumentationInfo("Code-label BFF DTO", "A string code/label pair for select options.")]
public sealed class CodeLabelBffDto
{
    public string Code { get; set; } = default!;
    public string Label { get; set; } = default!;
}

[DocumentationInfo("Value-label BFF DTO", "An integer value/label pair for select options backed by numeric enums.")]
public sealed class ValueLabelBffDto
{
    public int Value { get; set; }
    public string Label { get; set; } = default!;
}

[DocumentationInfo("Owner candidate BFF DTO", "A user/profile candidate for the vessel owner dropdown on the register page.")]
public sealed class OwnerCandidateBffDto
{
    public long UserId { get; set; }
    public long? ProfileId { get; set; }
    public string DisplayName { get; set; } = default!;
    public string? Email { get; set; }
    public string? AvatarUrl { get; set; }
}
