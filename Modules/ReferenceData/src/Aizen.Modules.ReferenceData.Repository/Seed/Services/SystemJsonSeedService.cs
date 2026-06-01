using Aizen.Modules.ReferenceData.Abstraction.Enum;
using Aizen.Modules.ReferenceData.Abstraction.Model;
using Aizen.Modules.ReferenceData.Domain.Entities.System;
using Aizen.Modules.ReferenceData.Domain.Entities.SystemParameter;
using Aizen.Modules.ReferenceData.Domain.Interface;
using Aizen.Modules.ReferenceData.Repository.Context;
using Aizen.Modules.ReferenceData.Repository.Seed.Models.System;
using Aizen.Modules.ReferenceData.Repository.Seed.Readers;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ReferenceData.Repository.Seed.Services;

/// <summary>Seeds system-level entities: languages, time zones, country phone codes, and system parameters.</summary>
[DocumentationInfo(
    "Seeds system definition entities from JSON files in the System/ folder.",
    "Covers LanguageEntity, TimeZoneEntity, CountryPhoneCodeEntity, and SystemParameterEntity. Uses DbContext directly for entities without dedicated repositories.")]
public sealed class SystemJsonSeedService
{
    private readonly ISystemParameterRepository _systemParameterRepository;
    private readonly ReferenceDataDbContext _dbContext;
    private readonly IReferenceDataJsonSeedReader _reader;

    public SystemJsonSeedService(
        ISystemParameterRepository systemParameterRepository,
        ReferenceDataDbContext dbContext,
        IReferenceDataJsonSeedReader reader)
    {
        _systemParameterRepository = systemParameterRepository;
        _dbContext = dbContext;
        _reader = reader;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedLanguagesAsync(cancellationToken);
        await SeedTimeZonesAsync(cancellationToken);
        await SeedCountryPhoneCodesAsync(cancellationToken);
        await SeedSystemParametersAsync(cancellationToken);
    }

    private async Task SeedLanguagesAsync(CancellationToken cancellationToken)
    {
        var models = await _reader.ReadListAsync<LanguageSeedModel>("System/languages.json", cancellationToken: cancellationToken);

        foreach (var model in models)
        {
            var existing = await _dbContext.Languages
                .FirstOrDefaultAsync(x => x.Code == model.Code.Trim().ToLowerInvariant(), cancellationToken);

            if (existing is null)
            {
                var entity = LanguageEntity.Create(model.Code, model.Name, model.NativeName, model.IsDefault);
                if (!model.IsActive) entity.IsActive = false;
                _dbContext.Languages.Add(entity);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedTimeZonesAsync(CancellationToken cancellationToken)
    {
        var models = await _reader.ReadListAsync<TimeZoneSeedModel>("System/time-zones.json", cancellationToken: cancellationToken);

        foreach (var model in models)
        {
            var existing = await _dbContext.TimeZones
                .FirstOrDefaultAsync(x => x.Code == model.Code.Trim(), cancellationToken);

            if (existing is null)
            {
                var entity = TimeZoneEntity.Create(model.Code, model.DisplayName, model.UtcOffset);
                if (!model.IsActive) entity.IsActive = false;
                _dbContext.TimeZones.Add(entity);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedCountryPhoneCodesAsync(CancellationToken cancellationToken)
    {
        var models = await _reader.ReadListAsync<CountryPhoneCodeSeedModel>("System/country-phone-codes.json", cancellationToken: cancellationToken);

        foreach (var model in models)
        {
            var normalizedCode = model.CountryCode.Trim().ToUpperInvariant();
            var existing = await _dbContext.CountryPhoneCodes
                .FirstOrDefaultAsync(x => x.CountryCode == normalizedCode, cancellationToken);

            if (existing is null)
            {
                var entity = CountryPhoneCodeEntity.Create(
                    model.CountryCode,
                    model.PhoneCode,
                    model.CountryName,
                    model.IsAllowedForRegistration,
                    model.IsAllowedForTransfer);

                if (!model.IsActive) entity.IsActive = false;
                _dbContext.CountryPhoneCodes.Add(entity);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedSystemParametersAsync(CancellationToken cancellationToken)
    {
        var models = await _reader.ReadListAsync<SystemParameterSeedModel>("System/system-parameters.json", cancellationToken: cancellationToken);

        foreach (var model in models)
        {
            var valueType = (SystemParameterValueType)model.ValueType;
            var existing = await _systemParameterRepository.GetByKeyAsync(model.Key, cancellationToken);

            if (existing is null)
            {
                var entity = SystemParameterEntity.Create(
                    model.Key,
                    model.Value,
                    valueType,
                    model.Description,
                    model.IsEncrypted);

                if (!model.IsActive) entity.Deactivate();
                await _systemParameterRepository.AddAsync(entity, cancellationToken);
            }
            else
            {
                existing.Update(model.Value, model.Description, model.IsActive);
                _systemParameterRepository.Update(existing);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
