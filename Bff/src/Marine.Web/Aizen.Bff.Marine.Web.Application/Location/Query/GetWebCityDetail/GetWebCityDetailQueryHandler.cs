using System.Net;
using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Contracts.Location;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Location.Query.GetWebCityDetail;

public sealed class GetWebCityDetailQueryHandler
    : AizenQueryHandler<GetWebCityDetailQuery, WebLocationDetailDto>
{
    private readonly IReferenceDataRemoteCall _reference;
    private readonly ILogger<GetWebCityDetailQueryHandler> _logger;

    public GetWebCityDetailQueryHandler(
        IReferenceDataRemoteCall reference, ILogger<GetWebCityDetailQueryHandler> logger)
    {
        _reference = reference;
        _logger = logger;
    }

    public override async Task<WebLocationDetailDto?> Handle(
        GetWebCityDetailQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var city = (await _reference.GetCity(request.CountryCode, request.CityCode))?.Body
                ?? throw new AizenBusinessException("The requested city was not found.");

            // Parent name for the breadcrumb — best-effort: a failure falls back to the code, never fails the request.
            var country = await TryGetCountry(request.CountryCode);
            return WebLocationMapper.ToCityDetail(city, country);
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new AizenBusinessException("The requested city was not found.");
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Web city detail for {Country}/{City} failed (status {Status}).",
                request.CountryCode, request.CityCode, ex.StatusCode);
            throw new AizenBusinessException("Location reference data is currently unavailable.");
        }
    }

    private async Task<Aizen.Modules.ReferenceData.Abstraction.Dto.Location.CountryDto?> TryGetCountry(string code)
    {
        try
        {
            return (await _reference.GetCountry(code))?.Body;
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogInformation(ex, "Parent country {Country} name unresolved; falling back to code.", code);
            return null;
        }
    }
}
