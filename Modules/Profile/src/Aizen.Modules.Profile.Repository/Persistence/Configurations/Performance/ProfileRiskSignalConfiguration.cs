using Aizen.Modules.Profile.Domain.Entities.Performance;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Profile.Repository.Persistence.Configurations.Performance;

public sealed class ProfileRiskSignalConfiguration
    : IEntityTypeConfiguration<ProfileRiskSignalEntity>
{
    public void Configure(EntityTypeBuilder<ProfileRiskSignalEntity> b)
    {
        b.ToTable("profile_risk_signals");
        b.HasKey(x => x.Id);

        b.Property(x => x.ProfileId).IsRequired();
        b.Property(x => x.ProfileType).HasConversion<int>().IsRequired();

        b.Property(x => x.Severity).HasConversion<int>().IsRequired();
        b.Property(x => x.SignalCode).HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasMaxLength(500).IsRequired();

        b.Property(x => x.SourceModule).HasMaxLength(100);
        b.Property(x => x.SourceEntityId);

        b.Property(x => x.DetectedAtUtc).IsRequired();
        b.Property(x => x.IsResolved).IsRequired();
        b.Property(x => x.ResolvedAtUtc);
        b.Property(x => x.ResolutionNote).HasMaxLength(1000);
        b.Property(x => x.ResolvedByUserId);

        // Most queries filter by (ProfileId, ProfileType, IsResolved)
        b.HasIndex(x => new { x.ProfileId, x.ProfileType, x.IsResolved })
         .HasDatabaseName("IX_profile_risk_signals_ProfileId_ProfileType_IsResolved");

        b.HasIndex(x => x.Severity)
         .HasDatabaseName("IX_profile_risk_signals_Severity");

        b.HasIndex(x => x.SignalCode)
         .HasDatabaseName("IX_profile_risk_signals_SignalCode");
    }
}
