using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Vessel;
using Aizen.Modules.Vessel.Abstraction.Dto.Vessel;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Vessel;

/// <summary>Shared projection module VesselDetailDto → the mobile detail contract. Used by the detail read
/// (M4a) and the create orchestration (M4b), which returns the freshly assembled vessel.</summary>
internal static class MobileVesselMapper
{
    public static MobileVesselDetailDto MapDetail(VesselDetailDto d)
    {
        var v = d.Vessel;
        var spec = d.Specification;
        var loc = d.CurrentLocation;

        return new MobileVesselDetailDto
        {
            Id = v.Id,
            Name = v.Name,
            TypeCode = v.VesselTypeCode,
            Flag = v.FlagCountryCode,
            Status = v.Status.ToString(),
            IsArchived = v.IsArchived,
            ArchivedAt = v.ArchivedAt,
            Description = v.Description,
            RegistrationNumber = v.RegistrationNumber,
            ImoNumber = v.ImoNumber,
            MmsiNumber = v.MmsiNumber,
            CallSign = v.CallSign,
            HomeMarinaName = v.HomeMarinaName,
            LengthMeters = spec?.LengthValue,
            BeamMeters = spec?.BeamValue,
            DraftMeters = spec?.DraftValue,
            GrossTonnage = spec?.GrossTonnage,
            HullMaterialCode = spec?.HullMaterialCode,
            Brand = spec?.Brand,
            Model = spec?.Model,
            ProductionYear = spec?.ProductionYear,
            CabinCount = spec?.CabinCount,
            MarinaName = loc?.MarinaName,
            Latitude = (double?)loc?.Latitude,
            Longitude = (double?)loc?.Longitude,
            Engines = d.Engines?.Select(e => new MobileVesselEngineDto
            {
                Name = e.EngineName,
                TypeCode = e.EngineTypeCode,
                FuelTypeCode = e.FuelTypeCode,
                HorsePower = e.HorsePower,
                IsPrimary = e.IsPrimary,
            }).ToList() ?? new List<MobileVesselEngineDto>(),
        };
    }
}
