using Aizen.Modules.Profile.Domain.Entities.Performance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Profile.Repository.Persistence.Configurations.Performance;

public sealed class ProfileScoreHistoryConfiguration
    : IEntityTypeConfiguration<ProfileScoreHistoryEntity>
{
    public void Configure(EntityTypeBuilder<ProfileScoreHistoryEntity> b)
    {
        b.ToTable("profile_score_history");
        b.HasKey(x => x.Id);

        b.Property(x => x.ProfileId).IsRequired();
        b.Property(x => x.ProfileType).HasConversion<int>().IsRequired();
        b.Property(x => x.PriorityTier).HasConversion<int>().IsRequired();

        b.Property(x => x.OverallScore)    .HasColumnType("numeric(6,2)").IsRequired();
        b.Property(x => x.ConfidenceScore) .HasColumnType("numeric(5,4)").IsRequired();
        b.Property(x => x.SampleSize).IsRequired();

        b.Property(x => x.RecordedAtUtc).IsRequired();
        b.Property(x => x.TriggerReason).HasMaxLength(500);
        b.Property(x => x.MetadataJson).HasMaxLength(2000);

        // History is append-only — no UPDATE/DELETE cascade needed

        b.HasIndex(x => new { x.ProfileId, x.ProfileType })
         .HasDatabaseName("IX_profile_score_history_ProfileId_ProfileType");

        b.HasIndex(x => x.RecordedAtUtc)
         .HasDatabaseName("IX_profile_score_history_RecordedAtUtc");

        b.HasIndex(x => x.PriorityTier)
         .HasDatabaseName("IX_profile_score_history_PriorityTier");
    }
}
