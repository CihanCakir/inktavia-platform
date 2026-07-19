using Aizen.Core.Infrastructure.Api;
using Aizen.Core.RemoteCall.Abstraction;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;

namespace Aizen.Bff.MarineProvider.Application.Common.RemoteClients;

public interface IReferenceDataRemoteCall : IAizenRemoteCall
{
    [AizenRemoteCallGet("/api/v1/reference-data/locations/{countryCode}/cities")]
    Task<AizenApiResponse<List<CityDto>>> GetCitiesByCountry(string countryCode, [Refit.Query] bool onlyActive = true);
}
