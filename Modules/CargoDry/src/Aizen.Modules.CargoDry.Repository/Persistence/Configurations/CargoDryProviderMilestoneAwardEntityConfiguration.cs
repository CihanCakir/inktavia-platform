using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryProviderMilestoneAwardEntityConfiguration : IEntityTypeConfiguration<CargoDryProviderMilestoneAwardEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryProviderMilestoneAwardEntity> builder)
    {
        builder.ToTable("provider_milestone_awards");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.MilestoneType).IsRequired().HasMaxLength(50);
        builder.Property(x => x.PeriodKey).IsRequired().HasMaxLength(50);
        builder.Property(x => x.DisplayValue).HasMaxLength(500);
        builder.HasIndex(x => new { x.ProviderProfileId, x.MilestoneType, x.PeriodKey }).IsUnique();
    }
}
