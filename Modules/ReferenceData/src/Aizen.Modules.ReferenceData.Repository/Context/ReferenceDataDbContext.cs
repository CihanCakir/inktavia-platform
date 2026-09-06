using Aizen.Core.EFCore;
using Aizen.Modules.ReferenceData.Domain.Entities.Currency;
using Aizen.Modules.ReferenceData.Domain.Entities.ExchangeRate;
using Aizen.Modules.ReferenceData.Domain.Entities.LookupGroup;
using Aizen.Modules.ReferenceData.Domain.Entities.Marina;
using Aizen.Modules.ReferenceData.Domain.Entities.Measurement;
using Aizen.Modules.ReferenceData.Domain.Entities.System;
using Aizen.Modules.ReferenceData.Domain.Entities.SystemParameter;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.ReferenceData.Repository.Context;

public sealed class ReferenceDataDbContext : AizenDbContext
{
    public ReferenceDataDbContext(DbContextOptions<ReferenceDataDbContext> options)
        : base(options)
    {
    }

    public DbSet<CurrencyEntity> Currencies => Set<CurrencyEntity>();
    public DbSet<ExchangeRateEntity> ExchangeRates => Set<ExchangeRateEntity>();
    public DbSet<ExchangeRateHistoryEntity> ExchangeRateHistories => Set<ExchangeRateHistoryEntity>();
    public DbSet<MeasurementUnitEntity> MeasurementUnits => Set<MeasurementUnitEntity>();
    public DbSet<LookupGroupEntity> LookupGroups => Set<LookupGroupEntity>();
    public DbSet<LookupItemEntity> LookupItems => Set<LookupItemEntity>();
    public DbSet<SystemParameterEntity> SystemParameters => Set<SystemParameterEntity>();
    public DbSet<LanguageEntity> Languages => Set<LanguageEntity>();
    public DbSet<TimeZoneEntity> TimeZones => Set<TimeZoneEntity>();
    public DbSet<CountryPhoneCodeEntity> CountryPhoneCodes => Set<CountryPhoneCodeEntity>();
    public DbSet<MarinaEntity> Marinas => Set<MarinaEntity>();
    // Brand/model catalog (vessel wizard) — flat, convention-mapped like the other ReferenceData entities.
    public DbSet<Domain.Entities.Catalog.VesselBrandEntity> VesselBrands => Set<Domain.Entities.Catalog.VesselBrandEntity>();
    public DbSet<Domain.Entities.Catalog.VesselModelEntity> VesselModels => Set<Domain.Entities.Catalog.VesselModelEntity>();
    public DbSet<Domain.Entities.Catalog.EngineBrandEntity> EngineBrands => Set<Domain.Entities.Catalog.EngineBrandEntity>();
    public DbSet<Domain.Entities.Catalog.EngineModelEntity> EngineModels => Set<Domain.Entities.Catalog.EngineModelEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("ref");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ReferenceDataDbContext).Assembly);
    }
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        NormalizeDateTimeProperties();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override int SaveChanges()
    {
        NormalizeDateTimeProperties();
        return base.SaveChanges();
    }

    private void NormalizeDateTimeProperties()
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dt && dt.Kind == DateTimeKind.Local)
                {
                    property.CurrentValue = dt.ToUniversalTime();
                }
            }
        }
    }
}
