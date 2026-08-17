using Aizen.Modules.Profile.Domain.Entities.Performance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Profile.Repository.Persistence.Configurations.Performance;

public sealed class ProfileDecisionLogConfiguration
    : IEntityTypeConfiguration<ProfileDecisionLogEntity>
{
    public void Configure(EntityTypeBuilder<ProfileDecisionLogEntity> b)
    {
        b.ToTable("profile_decision_logs");
        b.HasKey(x => x.Id);

        b.Property(x => x.ProfileId).IsRequired();
        b.Property(x => x.ProfileType).HasConversion<int>().IsRequired();

        b.Property(x => x.EventType).HasConversion<int>().IsRequired();
        b.Property(x => x.EventDescription).HasMaxLength(1000).IsRequired();

        b.Property(x => x.PreviousTier).HasConversion<int?>();
        b.Property(x => x.NewTier).HasConversion<int?>();
        b.Property(x => x.PreviousScore).HasColumnType("numeric(6,2)");
        b.Property(x => x.NewScore).HasColumnType("numeric(6,2)");

        b.Property(x => x.ActorUserId).HasMaxLength(200);
        b.Property(x => x.OccurredAtUtc).IsRequired();
        b.Property(x => x.MetadataJson).HasMaxLength(2000);

        // Decision log is append-only — no UPDATE/DELETE cascade needed

        b.HasIndex(x => new { x.ProfileId, x.ProfileType })
         .HasDatabaseName("IX_profile_decision_logs_ProfileId_ProfileType");

        b.HasIndex(x => x.EventType)
         .HasDatabaseName("IX_profile_decision_logs_EventType");

        b.HasIndex(x => x.OccurredAtUtc)
         .HasDatabaseName("IX_profile_decision_logs_OccurredAtUtc");
    }
}
