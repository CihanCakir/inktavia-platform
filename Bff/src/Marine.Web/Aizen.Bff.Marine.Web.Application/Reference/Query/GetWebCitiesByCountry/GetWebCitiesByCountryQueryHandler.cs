using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Reference.Query.GetWebCitiesByCountry;

public sealed class GetWebCitiesByCountryQueryHandler
    : AizenQueryHandler<GetWebCitiesByCountryQuery, List<CityDto>>
{
    private readonly IReferenceDataRemoteCall _reference;
    private readonly ILogger<GetWebCitiesByCountryQueryHandler> _logger;

    public GetWebCitiesByCountryQueryHandler(
        IReferenceDataRemoteCall reference, ILogger<GetWebCitiesByCountryQueryHandler> logger)
    {
        _reference = reference;
        _logger = logger;
    }

    public override async Task<List<CityDto>?> Handle(
        GetWebCitiesByCountryQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var resp = await _reference.GetCitiesByCountry(request.CountryCode);
            // Passthrough: reference DTO is clean, public lookup data.
            return resp?.Body ?? new List<CityDto>();
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "Get web cities failed — country {CountryCode} not found.", request.CountryCode);
            throw new AizenBusinessException("The requested country was not found.");
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Get web cities for {CountryCode} failed (status {Status}).",
                request.CountryCode, ex.StatusCode);
            throw new AizenBusinessException("City reference data is currently unavailable.");
        }
    }
}
