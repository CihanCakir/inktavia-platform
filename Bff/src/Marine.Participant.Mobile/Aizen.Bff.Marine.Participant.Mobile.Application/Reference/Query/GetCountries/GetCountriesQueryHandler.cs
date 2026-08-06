using Aizen.Bff.Marine.Participant.Mobile.Application.Common.RemoteClients;
using Aizen.Bff.Marine.Participant.Mobile.Application.Contracts.Reference;
using Aizen.Core.CQRS.Handler;
using Microsoft.Extensions.Logging;

namespace Aizen.Bff.Marine.Participant.Mobile.Application.Reference;

public sealed class GetCountriesQueryHandler
    : AizenQueryHandler<GetCountriesQuery, List<CountryItemDto>>
{
    private readonly IReferenceDataRemoteCall _reference;
    private readonly ILogger<GetCountriesQueryHandler> _logger;

    public GetCountriesQueryHandler(
        IReferenceDataRemoteCall reference, ILogger<GetCountriesQueryHandler> logger)
    {
        _reference = reference;
        _logger = logger;
    }

    public override async Task<List<CountryItemDto>?> Handle(
        GetCountriesQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _reference.GetCountries();
            var items = result?.Body ?? new List<Aizen.Modules.ReferenceData.Abstraction.Dto.Location.CountryDto>();
            return items
                .OrderBy(c => c.Name)
                .Select(c => new CountryItemDto
                {
                    Code = c.CountryCode,
                    Name = c.Name,
                    DialCode = c.PhoneCode,
                    CurrencyCode = c.DefaultCurrencyCode,
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reference countries lookup failed.");
            return new List<CountryItemDto>();
        }
    }
}
