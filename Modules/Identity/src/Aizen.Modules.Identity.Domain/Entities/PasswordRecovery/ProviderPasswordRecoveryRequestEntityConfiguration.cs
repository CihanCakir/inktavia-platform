using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Identity.Domain.Entities.PasswordRecovery;

public class ProviderPasswordRecoveryRequestEntityConfiguration
    : IEntityTypeConfiguration<ProviderPasswordRecoveryRequestEntity>
{
    public void Configure(EntityTypeBuilder<ProviderPasswordRecoveryRequestEntity> builder)
    {
        builder.ToTable("provider_password_recovery_requests");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ResetRequestId)
               .IsRequired()
               .HasMaxLength(64);

        builder.HasIndex(x => x.ResetRequestId)
               .IsUnique();

        builder.Property(x => x.KeycloakSubjectId)
               .IsRequired()
               .HasMaxLength(128);

        builder.Property(x => x.UserId)
               .IsRequired();

        builder.Property(x => x.Channel)
               .IsRequired()
               .HasMaxLength(16);

        builder.Property(x => x.TargetHash)
               .IsRequired()
               .HasMaxLength(64);

        builder.Property(x => x.MaskedTarget)
               .IsRequired()
               .HasMaxLength(256);

        builder.Property(x => x.OtpHash)
               .IsRequired()
               .HasMaxLength(64);

        builder.Property(x => x.OtpSalt)
               .IsRequired()
               .HasMaxLength(32);

        builder.Property(x => x.OtpExpiresAtUtc).IsRequired();
        builder.Property(x => x.Attempts).IsRequired();
        builder.Property(x => x.MaxAttempts).IsRequired();

        builder.Property(x => x.ResetTokenHash).HasMaxLength(64);
        builder.Property(x => x.ResetTokenSalt).HasMaxLength(32);

        builder.Property(x => x.LastSentAtUtc).IsRequired();
    }
}
