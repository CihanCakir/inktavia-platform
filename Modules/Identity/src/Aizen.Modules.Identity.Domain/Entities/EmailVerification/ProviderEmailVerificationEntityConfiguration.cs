using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Identity.Domain.Entities.EmailVerification;

public class ProviderEmailVerificationEntityConfiguration
    : IEntityTypeConfiguration<ProviderEmailVerificationEntity>
{
    public void Configure(EntityTypeBuilder<ProviderEmailVerificationEntity> builder)
    {
        builder.ToTable("provider_email_verifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        // 32 baytlık PBKDF2 özeti/tuzu Base64 olarak saklanır (parola kurtarma ile aynı boyutlandırma).
        builder.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
        builder.Property(x => x.TokenSalt).IsRequired().HasMaxLength(32);
        builder.Property(x => x.ExpiresAt).IsRequired();
        // Kullanıcı bazında bekleyen kaydı bulmak (yeniden gönderme / eskiyi geçersizleme) için indeks.
        builder.HasIndex(x => x.UserId);
    }
}
