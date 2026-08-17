using Aizen.Modules.ReferenceData.Repository.Seed.Models.Currency;
using Aizen.Modules.ReferenceData.Repository.Seed.Readers;

namespace Aizen.Modules.ReferenceData.Repository.Seed.Services;

/// <summary>Seeds exchange rate entities from JSON files.</summary>
[DocumentationInfo(
    "Seeds ExchangeRateEntity records from exchange-rates.json.",
    "Exchange rates are skipped if the file is empty (rates are managed via live provider). Template file only.")]
public sealed class ExchangeRateJsonSeedService
{
    private readonly IReferenceDataJsonSeedReader _reader;

    public ExchangeRateJsonSeedService(IReferenceDataJsonSeedReader reader)
    {
        _reader = reader;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var models = await _reader.ReadListAsync<ExchangeRateSeedModel>(
            "ExchangeRate/exchange-rates.json",
            optional: true,
            cancellationToken: cancellationToken);

        // Exchange rates are managed by a live provider. The seed file is a template.
        // Nothing to insert if the file is empty.
        _ = models;
    }
}
