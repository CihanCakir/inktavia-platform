using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Identity.Domain.Entities.UserExternalLogin
{
  public sealed class UserExternalLoginEntityConfiguration : IEntityTypeConfiguration<UserExternalLoginEntity>
    {
        public void Configure(EntityTypeBuilder<UserExternalLoginEntity> b)
        {
            // Tablo
            b.ToTable("UserExternalLogins");

            // Key
            b.HasKey(x => x.Id);

            // İndeksler
            b.HasIndex(x => x.UserId);
            b.HasIndex(x => new { x.Provider, x.ProviderUserId }).IsUnique();

            // Enum -> int
            b.Property(x => x.Provider)
             .HasConversion<int>()
             .IsRequired();

            // Alanlar
            b.Property(x => x.ProviderUserId)
             .HasMaxLength(256)
             .IsRequired();

            b.Property(x => x.EmailAtLinkTime)
             .HasMaxLength(320);                 // RFC için güvenli üst sınır

            b.Property(x => x.Scope)
             .HasMaxLength(512);

            // Audit / Soft-delete alanları projene göre ayarla
            b.Property(x => x.CreatedAt).IsRequired();
            b.Property(x => x.ModifiedAt);
            b.Property(x => x.IsDeleted).HasDefaultValue(false);
            b.Property(x => x.DeletedAt);

            // İlişki: User 1 - n ExternalLogins
            b.HasOne(x => x.User)
             .WithMany(u => u.ExternalLogins)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);

            // (Varsa) Global soft-delete filtresi — entity’de IsDeleted alanı mevcutsa aç
            b.HasQueryFilter(x => !x.IsDeleted);
        }
    }
}