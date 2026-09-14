using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryOwnerPreferredProviderEntityConfiguration
    : IEntityTypeConfiguration<CargoDryOwnerPreferredProviderEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryOwnerPreferredProviderEntity> builder)
    {
        builder.ToTable("owner_preferred_providers");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.OwnerUserId).IsRequired();
        builder.Property(x => x.ProviderProfileId).IsRequired();
        builder.Property(x => x.FirstServiceRequestId).IsRequired();
        builder.Property(x => x.SetAtUtc).IsRequired();

        // One preferred provider per owner (set-once).
        builder.HasIndex(x => x.OwnerUserId).IsUnique();
        builder.HasIndex(x => x.ProviderProfileId);

        // IsActive / CreateDate / ModifyDate: mapped by AizenEntityWithAudit base configuration
    }
}
