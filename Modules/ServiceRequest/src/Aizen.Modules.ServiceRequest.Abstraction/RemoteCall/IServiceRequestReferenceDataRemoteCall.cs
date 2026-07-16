using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;

namespace Aizen.Modules.ServiceRequest.Abstraction.RemoteCall;

public interface IServiceRequestReferenceDataRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/reference-data/locations/{countryCode}/cities/{cityCode}")]
    Task<AizenApiResponse<SrCityValidationDto>> GetCity(string countryCode, string cityCode);

    [AizenRemoteCallGet("/api/v1/reference-data/measurement-units?onlyActive=true")]
    Task<AizenApiResponse<List<SrMeasurementUnitDto>>> GetActiveMeasurementUnits();
}

public sealed class SrCityValidationDto
{
    public string CityCode { get; set; } = default!;
    public string CountryCode { get; set; } = default!;
    public bool IsActive { get; set; }
}

public sealed class SrMeasurementUnitDto
{
    public string Code { get; set; } = default!;
    public bool IsActive { get; set; }
}
