using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Identity.Domain.Entities
{
       public sealed class UserMessagePermissionEntityConfiguration : IEntityTypeConfiguration<UserMessagePermissionEntity>
       {
              public void Configure(EntityTypeBuilder<UserMessagePermissionEntity> b)
              {
                     b.ToTable("UserMessagePermissions");

                     b.HasKey(x => x.Id);

                     // enum -> int
                     b.Property(x => x.PermissionType)
                      .HasConversion<int>()
                      .IsRequired();

                     b.Property(x => x.PermissionContentId).IsRequired(false);
                     b.Property(x => x.PermissionValue).IsRequired(false);
                     b.Property(x => x.IsActive).HasDefaultValue(true);
                     b.Property(x => x.IYSStatus).HasDefaultValue(false);

                     // Unique key: aynı kullanıcı + aynı tür + aynı içerik için tek kayıt
                     b.HasIndex(x => new { x.UserId, x.PermissionType, x.PermissionContentId })
                      .IsUnique();

                     // ilişkiler
                     b.HasOne(x => x.User)
                      .WithMany(u => u.UserMessagePermissions!)
                      .HasForeignKey(x => x.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                     b.HasOne(x => x.ActiveProfile)
                      .WithMany()
                      .HasForeignKey(x => x.ActiveProfileId)
                      .OnDelete(DeleteBehavior.NoAction);
              }
       }
}
