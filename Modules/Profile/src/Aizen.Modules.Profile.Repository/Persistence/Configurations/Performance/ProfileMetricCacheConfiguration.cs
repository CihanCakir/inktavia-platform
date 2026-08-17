using Aizen.Modules.Profile.Domain.Entities.Performance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Profile.Repository.Persistence.Configurations.Performance;

public sealed class ProfileMetricCacheConfiguration
    : IEntityTypeConfiguration<ProfileMetricCacheEntity>
{
    public void Configure(EntityTypeBuilder<ProfileMetricCacheEntity> b)
    {
        b.ToTable("profile_metric_caches");
        b.HasKey(x => x.Id);

        b.Property(x => x.ProfileId).IsRequired();
        b.Property(x => x.ProfileType).HasConversion<int>().IsRequired();

        b.Property(x => x.MetricKey).HasMaxLength(200).IsRequired();
        b.Property(x => x.MetricValueJson).HasMaxLength(2000).IsRequired();

        b.Property(x => x.CachedAtUtc).IsRequired();
        b.Property(x => x.ExpiresAtUtc);

        // One cache entry per (ProfileId, ProfileType, MetricKey)
        b.HasIndex(x => new { x.ProfileId, x.ProfileType, x.MetricKey })
         .IsUnique()
         .HasDatabaseName("UIX_profile_metric_caches_ProfileId_ProfileType_MetricKey");

        b.HasIndex(x => x.ExpiresAtUtc)
         .HasDatabaseName("IX_profile_metric_caches_ExpiresAtUtc");
    }
}
