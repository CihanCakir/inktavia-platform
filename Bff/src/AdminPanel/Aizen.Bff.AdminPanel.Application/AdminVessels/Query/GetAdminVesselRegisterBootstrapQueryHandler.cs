using Aizen.Bff.AdminPanel.Application.AdminVessels.Dto;
using Aizen.Bff.AdminPanel.Application.Common.RemoteClients;
using Aizen.Bff.AdminPanel.Application.Common.Warnings;
using Aizen.Core.CQRS.Handler;

namespace Aizen.Bff.AdminPanel.Application.AdminVessels.Query;

[DocumentationInfo("Get admin vessel register bootstrap query handler", "Builds the Vessel Register page options from ReferenceData (countries), Identity (owner candidates), and static enum mappings. Gracefully degrades with warnings if a module is unavailable.")]
public sealed class GetAdminVesselRegisterBootstrapQueryHandler
    : AizenQueryHandler<GetAdminVesselRegisterBootstrapQuery, AdminVesselRegisterBootstrapBffResponse>
{
    private readonly IIdentityAdminBffRemoteCall _identity;
    private readonly IReferenceDataAdminBffRemoteCall _referenceData;

    // Static enum-backed option lists.
    private static readonly List<ValueLabelBffDto> AssetTypes = new()
    {
        new() { Value = 1, Label = "Motor Yacht" },
        new() { Value = 2, Label = "Sailing Yacht" },
        new() { Value = 3, Label = "Superyacht" },
        new() { Value = 4, Label = "Catamaran" },
        new() { Value = 5, Label = "RIB" },
        new() { Value = 6, Label = "Commercial" }
    };

    private static readonly List<ValueLabelBffDto> OperationalStatuses = new()
    {
        new() { Value = 1, Label = "In Service" },
        new() { Value = 2, Label = "Refit" },
        new() { Value = 3, Label = "Idle" },
        new() { Value = 4, Label = "Decommissioned" }
    };

    private static readonly List<ValueLabelBffDto> OwnershipStatuses = new()
    {
        new() { Value = 1, Label = "Private" },
        new() { Value = 2, Label = "Charter" },
        new() { Value = 3, Label = "Corporate" }
    };

    private static readonly List<CodeLabelBffDto> VesselTypes = new()
    {
        new() { Code = "MOTOR_YACHT", Label = "Motor Yacht" },
        new() { Code = "SAILING_YACHT", Label = "Sailing Yacht" },
        new() { Code = "SUPERYACHT", Label = "Superyacht" },
        new() { Code = "CATAMARAN", Label = "Catamaran" },
        new() { Code = "RIB", Label = "RIB" },
        new() { Code = "CARGO", Label = "Cargo" },
        new() { Code = "TUGBOAT", Label = "Tugboat" },
        new() { Code = "FERRY", Label = "Ferry" },
        new() { Code = "FISHING", Label = "Fishing" },
        new() { Code = "OTHER", Label = "Other" }
    };

    private static readonly List<CodeLabelBffDto> HullMaterials = new()
    {
        new() { Code = "GRP", Label = "GRP (Glass Reinforced Plastic)" },
        new() { Code = "ALUMINIUM", Label = "Aluminium" },
        new() { Code = "STEEL", Label = "Steel" },
        new() { Code = "WOOD", Label = "Wood" },
        new() { Code = "CARBON_FIBRE", Label = "Carbon Fibre" },
        new() { Code = "FERRO_CEMENT", Label = "Ferro Cement" }
    };

    private static readonly List<CodeLabelBffDto> SuperstructureMaterials = new()
    {
        new() { Code = "GRP", Label = "GRP" },
        new() { Code = "ALUMINIUM", Label = "Aluminium" },
        new() { Code = "WOOD", Label = "Wood" },
        new() { Code = "CARBON_FIBRE", Label = "Carbon Fibre" }
    };

    public GetAdminVesselRegisterBootstrapQueryHandler(
        IIdentityAdminBffRemoteCall identity,
        IReferenceDataAdminBffRemoteCall referenceData)
    {
        _identity = identity;
        _referenceData = referenceData;
    }

    public override async Task<AdminVesselRegisterBootstrapBffResponse?> Handle(
        GetAdminVesselRegisterBootstrapQuery request, CancellationToken cancellationToken)
    {
        var response = new AdminVesselRegisterBootstrapBffResponse
        {
            VesselRegister = new VesselRegisterBootstrapBffDto
            {
                Defaults = new VesselRegisterDefaultsBffDto(),
                Options = new VesselRegisterOptionsBffDto
                {
                    VesselTypes = VesselTypes,
                    AssetTypes = AssetTypes,
                    OperationalStatuses = OperationalStatuses,
                    OwnershipStatuses = OwnershipStatuses,
                    HullMaterials = HullMaterials,
                    SuperstructureMaterials = SuperstructureMaterials
                }
            }
        };

        await Task.WhenAll(
            TryLoadCountriesAsync(response, cancellationToken),
            TryLoadOwnerCandidatesAsync(response, cancellationToken));

        return response;
    }

        catch (Exception)
        {
            response.Warnings.Add(AdminBffWarning.CallFailed("ServiceToken", "Could not acquire Keycloak service token."));
            return null;
        }
    }

    private async Task TryLoadCountriesAsync(
        AdminVesselRegisterBootstrapBffResponse response,
        CancellationToken ct)
    {
        try
        {
            var result = await _referenceData.GetCountries();
            var countries = result?.Body;

            if (countries != null && countries.Count > 0)
            {
                var countryOptions = countries
                    .Select(c => new CodeLabelBffDto { Code = c.CountryCode ?? string.Empty, Label = c.Name ?? c.CountryCode ?? string.Empty })
                    .Where(c => !string.IsNullOrWhiteSpace(c.Code))
                    .OrderBy(c => c.Label)
                    .ToList();

                response.VesselRegister!.Options.FlagCountries = countryOptions;
                response.VesselRegister!.Options.BuildCountries = countryOptions;
            }
        }
        catch (Exception)
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("ReferenceData"));
        }
    }

    private async Task TryLoadOwnerCandidatesAsync(
        AdminVesselRegisterBootstrapBffResponse response,
        CancellationToken ct)
    {
        try
        {
            var result = await _identity.SearchProfiles(pageIndex: 0, pageSize: 100);
            var profiles = result?.Body?.Items;

            if (profiles != null && profiles.Count > 0)
            {
                response.VesselRegister!.Options.OwnerCandidates = profiles
                    .Select(p => new OwnerCandidateBffDto
                    {
                        UserId = p.UserId,
                        ProfileId = p.Id,
                        DisplayName = $"{p.FirstName} {p.LastName}".Trim(),
                        AvatarUrl = p.ProfilePhotoUrl
                    })
                    .ToList();
            }
        }
        catch (Exception)
        {
            response.Warnings.Add(AdminBffWarning.ModuleUnavailable("Identity"));
        }
    }
}
