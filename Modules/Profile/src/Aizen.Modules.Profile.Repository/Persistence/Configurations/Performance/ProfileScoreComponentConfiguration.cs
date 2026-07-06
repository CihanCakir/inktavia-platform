using Aizen.Modules.Profile.Domain.Entities.Performance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Profile.Repository.Persistence.Configurations.Performance;

public sealed class ProfileScoreComponentConfiguration
    : IEntityTypeConfiguration<ProfileScoreComponentEntity>
{
    public void Configure(EntityTypeBuilder<ProfileScoreComponentEntity> b)
    {
        b.ToTable("profile_score_components");
        b.HasKey(x => x.Id);

        b.Property(x => x.SnapshotId).IsRequired();
        b.Property(x => x.ProfileId).IsRequired();
        b.Property(x => x.ProfileType).HasConversion<int>().IsRequired();
        b.Property(x => x.Category).HasConversion<int>().IsRequired();

        b.Property(x => x.RawScore)             .HasColumnType("numeric(6,2)").IsRequired();
        b.Property(x => x.Weight)               .HasColumnType("numeric(5,4)").IsRequired();
        b.Property(x => x.WeightedContribution) .HasColumnType("numeric(8,4)").IsRequired();

        b.Property(x => x.MetricCount).IsRequired();
        b.Property(x => x.MetricsJson).HasMaxLength(4000);
        b.Property(x => x.CalculatedAtUtc).IsRequired();

        b.HasIndex(x => x.SnapshotId)
         .HasDatabaseName("IX_profile_score_components_SnapshotId");

        b.HasIndex(x => new { x.ProfileId, x.ProfileType })
         .HasDatabaseName("IX_profile_score_components_ProfileId_ProfileType");

        // One component per (SnapshotId, Category)
        b.HasIndex(x => new { x.SnapshotId, x.Category })
         .IsUnique()
         .HasDatabaseName("UIX_profile_score_components_SnapshotId_Category");
    }
}
