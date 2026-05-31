using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ReferenceData.Domain.Entities.Currency;

public sealed class CurrencyEntityConfiguration : IEntityTypeConfiguration<CurrencyEntity>
{
    public void Configure(EntityTypeBuilder<CurrencyEntity> builder)
    {
        builder.ToTable("currencies", "ref");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(3).IsRequired();
        builder.Property(x => x.NumericCode).HasMaxLength(10).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Symbol).HasMaxLength(10).IsRequired();
        builder.Property(x => x.DecimalPlaces).IsRequired();
        builder.Property(x => x.IsBaseCurrency).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasIndex(x => x.NumericCode).IsUnique();
    }
}
