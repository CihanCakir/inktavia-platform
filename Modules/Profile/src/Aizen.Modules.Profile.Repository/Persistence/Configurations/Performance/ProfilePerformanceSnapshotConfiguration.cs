using Aizen.Modules.Profile.Domain.Entities.Performance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Profile.Repository.Persistence.Configurations.Performance;

public sealed class ProfilePerformanceSnapshotConfiguration
    : IEntityTypeConfiguration<ProfilePerformanceSnapshotEntity>
{
    public void Configure(EntityTypeBuilder<ProfilePerformanceSnapshotEntity> b)
    {
        b.ToTable("profile_performance_snapshots");
        b.HasKey(x => x.Id);

        // ── Identity ──────────────────────────────────────────────────────────
        b.Property(x => x.ProfileId).IsRequired();
        b.Property(x => x.ProfileType).HasConversion<int>().IsRequired();

        // ── Tier ──────────────────────────────────────────────────────────────
        b.Property(x => x.PriorityTier).HasConversion<int>().IsRequired();

        // ── Scores ────────────────────────────────────────────────────────────
        b.Property(x => x.OverallScore)              .HasColumnType("numeric(6,2)").IsRequired();
        b.Property(x => x.ServiceRequestScore)       .HasColumnType("numeric(6,2)").IsRequired();
        b.Property(x => x.CargoDryScore)             .HasColumnType("numeric(6,2)").IsRequired();
        b.Property(x => x.OperationalDisciplineScore).HasColumnType("numeric(6,2)").IsRequired();
        b.Property(x => x.FinancialReliabilityScore) .HasColumnType("numeric(6,2)").IsRequired();
        b.Property(x => x.PlatformComplianceScore)   .HasColumnType("numeric(6,2)").IsRequired();
        b.Property(x => x.RiskPenaltyScore)          .HasColumnType("numeric(6,2)").IsRequired();

        // ── Confidence ────────────────────────────────────────────────────────
        b.Property(x => x.ConfidenceScore).HasColumnType("numeric(5,4)").IsRequired();
        b.Property(x => x.ConfidenceLevel).HasConversion<int>().IsRequired();
        b.Property(x => x.SampleSize).IsRequired();

        // ── Risk ──────────────────────────────────────────────────────────────
        b.Property(x => x.HasActiveRiskSignal).IsRequired();
        b.Property(x => x.ActiveRiskSignalMaxSeverity).HasConversion<int?>();

        // ── Timestamps ────────────────────────────────────────────────────────
        b.Property(x => x.LastCalculatedAtUtc);
        b.Property(x => x.ValidFromUtc);

        // ── Metadata ──────────────────────────────────────────────────────────
        b.Property(x => x.MetadataJson).HasMaxLength(2000);

        // ── Indexes ───────────────────────────────────────────────────────────
        // One snapshot per (ProfileId, ProfileType) — unique current-state row
        b.HasIndex(x => new { x.ProfileId, x.ProfileType })
         .IsUnique()
         .HasDatabaseName("UIX_profile_performance_snapshots_ProfileId_ProfileType");

        b.HasIndex(x => x.PriorityTier)
         .HasDatabaseName("IX_profile_performance_snapshots_PriorityTier");

        b.HasIndex(x => x.HasActiveRiskSignal)
         .HasDatabaseName("IX_profile_performance_snapshots_HasActiveRiskSignal");
    }
}
