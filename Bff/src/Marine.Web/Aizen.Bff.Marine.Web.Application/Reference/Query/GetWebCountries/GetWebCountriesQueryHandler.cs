using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Reference.Query.GetWebCountries;

public sealed class GetWebCountriesQueryHandler
    : AizenQueryHandler<GetWebCountriesQuery, List<CountryDto>>
{
    private readonly IReferenceDataRemoteCall _reference;
    private readonly ILogger<GetWebCountriesQueryHandler> _logger;

    public GetWebCountriesQueryHandler(
        IReferenceDataRemoteCall reference, ILogger<GetWebCountriesQueryHandler> logger)
    {
        _reference = reference;
        _logger = logger;
    }

    public override async Task<List<CountryDto>?> Handle(
        GetWebCountriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var resp = await _reference.GetCountries();
            // Passthrough: reference DTO is clean, public lookup data.
            return resp?.Body ?? new List<CountryDto>();
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Get web countries failed (status {Status}).", ex.StatusCode);
            throw new AizenBusinessException("Country reference data is currently unavailable.");
        }
    }
}
