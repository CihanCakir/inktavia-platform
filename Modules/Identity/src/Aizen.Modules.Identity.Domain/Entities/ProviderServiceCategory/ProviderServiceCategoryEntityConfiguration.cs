using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Identity.Domain.Entities.ProviderServiceCategory;

public sealed class ProviderServiceCategoryEntityConfiguration
    : IEntityTypeConfiguration<ProviderServiceCategoryEntity>
{
    public void Configure(EntityTypeBuilder<ProviderServiceCategoryEntity> builder)
    {
        builder.ToTable("provider_service_categories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProfileId).IsRequired();
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.ServiceCategoryCode).HasMaxLength(64).IsRequired();

        // One row per (profile, category); fast lookup by category for the area read-model.
        builder.HasIndex(x => new { x.ProfileId, x.ServiceCategoryCode }).IsUnique();
        builder.HasIndex(x => x.ServiceCategoryCode);
    }
}
