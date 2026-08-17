using System.Net;
using Aizen.Bff.Marine.Web.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Web.Application.Contracts.Location;
using Aizen.Core.CQRS.Handler;
using Aizen.Core.Infrastructure.Exception;
using Aizen.Modules.ReferenceData.Abstraction.Dto.Location;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Web.Application.Location.Query.GetWebDistrictDetail;

public sealed class GetWebDistrictDetailQueryHandler
    : AizenQueryHandler<GetWebDistrictDetailQuery, WebLocationDetailDto>
{
    private readonly IReferenceDataRemoteCall _reference;
    private readonly ILogger<GetWebDistrictDetailQueryHandler> _logger;

    public GetWebDistrictDetailQueryHandler(
        IReferenceDataRemoteCall reference, ILogger<GetWebDistrictDetailQueryHandler> logger)
    {
        _reference = reference;
        _logger = logger;
    }

    public override async Task<WebLocationDetailDto?> Handle(
        GetWebDistrictDetailQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // No single-district endpoint on the module — resolve the code against the districts-by-city list.
            var districts = (await _reference.GetDistrictsByCity(request.CountryCode, request.CityCode))?.Body ?? new();
            var district = districts.FirstOrDefault(d =>
                string.Equals(d.DistrictCode, request.DistrictCode, StringComparison.OrdinalIgnoreCase));
            if (district is null)
                throw new AizenBusinessException("The requested district was not found.");

            // Parent names for the breadcrumb — best-effort, code fallback on failure.
            var city = await TryGet(() => _reference.GetCity(request.CountryCode, request.CityCode), "city");
            var country = await TryGet(() => _reference.GetCountry(request.CountryCode), "country");
            return WebLocationMapper.ToDistrictDetail(district, city, country);
        }
        catch (Refit.ApiException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            throw new AizenBusinessException("The requested district was not found.");
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogWarning(ex, "Web district detail for {Country}/{City}/{District} failed (status {Status}).",
                request.CountryCode, request.CityCode, request.DistrictCode, ex.StatusCode);
            throw new AizenBusinessException("Location reference data is currently unavailable.");
        }
    }

    private async Task<T?> TryGet<T>(Func<Task<Core.Infrastructure.Api.AizenApiResponse<T?>>> call, string what)
        where T : class
    {
        try
        {
            return (await call())?.Body;
        }
        catch (Refit.ApiException ex)
        {
            _logger.LogInformation(ex, "Parent {What} name unresolved; falling back to code.", what);
            return null;
        }
    }
}
