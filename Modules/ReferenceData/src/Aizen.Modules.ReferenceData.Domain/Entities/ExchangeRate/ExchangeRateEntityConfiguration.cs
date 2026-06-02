using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ReferenceData.Domain.Entities.ExchangeRate;

public sealed class ExchangeRateEntityConfiguration : IEntityTypeConfiguration<ExchangeRateEntity>
{
    public void Configure(EntityTypeBuilder<ExchangeRateEntity> builder)
    {
        builder.ToTable("exchange_rates", "ref");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FromCurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.ToCurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Rate).HasPrecision(18, 8).IsRequired();
        builder.Property(x => x.ProviderType).IsRequired();
        builder.Property(x => x.RateDate).IsRequired();
        builder.Property(x => x.ValidUntil).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => new { x.FromCurrencyCode, x.ToCurrencyCode }).IsUnique();
        builder.HasIndex(x => x.RateDate);
    }
}
