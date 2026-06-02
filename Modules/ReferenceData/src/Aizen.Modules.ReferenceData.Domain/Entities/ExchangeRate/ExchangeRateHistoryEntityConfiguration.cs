using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ReferenceData.Domain.Entities.ExchangeRate;

public sealed class ExchangeRateHistoryEntityConfiguration : IEntityTypeConfiguration<ExchangeRateHistoryEntity>
{
    public void Configure(EntityTypeBuilder<ExchangeRateHistoryEntity> builder)
    {
        builder.ToTable("exchange_rate_histories", "ref");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromCurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.ToCurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Rate).HasPrecision(18, 8).IsRequired();
        builder.Property(x => x.ProviderType).IsRequired();
        builder.Property(x => x.RateDate).IsRequired();
        builder.Property(x => x.RawProviderPayload).HasColumnType("jsonb");
        builder.HasIndex(x => new { x.FromCurrencyCode, x.ToCurrencyCode, x.RateDate });
    }
}
