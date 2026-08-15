using System.Net;
using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Contracts.Location;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Location.Query.GetWebCountryDetail;

public sealed class GetWebCountryDetailQueryHandler
    : AizenQueryHandler<GetWebCountryDetailQuery, WebLocationDetailDto>
{
    private readonly IReferenceDataRemoteCall _reference;
    private readonly ILogger<GetWebCountryDetailQueryHandler> _logger;

    public GetWebCountryDetailQueryHandler(
        IReferenceDataRemoteCall reference, ILogger<GetWebCountryDetailQueryHandler> logger)
    {
        _reference = reference;
        _logger = logger;
    }

    public override async Task<WebLocationDetailDto?> Handle(
        GetWebCountryDetailQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var country = (await _reference.GetCountry(request.CountryCode))?.Body
                ?? throw new AizenBusinessException("The requested country was not found.");
            return WebLocationMapper.ToCountryDetail(country);
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new AizenBusinessException("The requested country was not found.");
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Web country detail for {CountryCode} failed (status {Status}).",
                request.CountryCode, ex.StatusCode);
            throw new AizenBusinessException("Location reference data is currently unavailable.");
        }
    }
}
