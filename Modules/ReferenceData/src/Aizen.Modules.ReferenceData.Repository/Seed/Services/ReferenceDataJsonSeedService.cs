using Aizen.Modules.ReferenceData.Domain.Interface.Service;
using Aizen.Modules.ReferenceData.Repository.Options;
using Microsoft.Extensions.Options;

namespace Aizen.Modules.ReferenceData.Repository.Seed.Services;

/// <summary>Orchestrator seed service that coordinates all ReferenceData JSON seed operations.</summary>
[DocumentationInfo(
    "Orchestrates all ReferenceData JSON seed phases in the correct order.",
    "Execution order: Currency → Measurement → System definitions → Lookup groups → Lookup items → Exchange rates → Location (country, cities, districts, neighborhoods, streets).")]
public sealed class ReferenceDataJsonSeedService : IReferenceDataJsonSeedService
{
    private readonly ReferenceDataSeedOptions _options;
    private readonly CurrencyJsonSeedService _currencyService;
    private readonly ExchangeRateJsonSeedService _exchangeRateService;
    private readonly MeasurementJsonSeedService _measurementService;
    private readonly LookupJsonSeedService _lookupService;
    private readonly SystemJsonSeedService _systemService;
    private readonly LocationJsonSeedService _locationService;

    public ReferenceDataJsonSeedService(
        IOptions<ReferenceDataSeedOptions> options,
        CurrencyJsonSeedService currencyService,
        ExchangeRateJsonSeedService exchangeRateService,
        MeasurementJsonSeedService measurementService,
        LookupJsonSeedService lookupService,
        SystemJsonSeedService systemService,
        LocationJsonSeedService locationService)
    {
        _options = options.Value;
        _currencyService = currencyService;
        _exchangeRateService = exchangeRateService;
        _measurementService = measurementService;
        _lookupService = lookupService;
        _systemService = systemService;
        _locationService = locationService;
    }

    public async Task SeedAllAsync(CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled) return;

        if (_options.SeedEfEntities)
            await SeedEfEntitiesAsync(cancellationToken);

        if (_options.SeedLocationDocuments)
            await SeedLocationDocumentsAsync(cancellationToken);
    }

    public async Task SeedEfEntitiesAsync(CancellationToken cancellationToken = default)
    {
        await _currencyService.SeedAsync(cancellationToken);
        await _measurementService.SeedAsync(cancellationToken);
        await _systemService.SeedAsync(cancellationToken);
        await SeedLookupTreeAsync(cancellationToken);
        await _exchangeRateService.SeedAsync(cancellationToken);
    }

    public async Task SeedLocationDocumentsAsync(CancellationToken cancellationToken = default)
    {
        await _locationService.SeedAsync(cancellationToken);
    }

    public async Task SeedLookupTreeAsync(CancellationToken cancellationToken = default)
    {
        await _lookupService.SeedGroupsAsync(cancellationToken);
        await _lookupService.SeedItemsAsync(cancellationToken);
    }
}
