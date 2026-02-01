using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class UserDeviceEntityConfiguration : IEntityTypeConfiguration<UserDeviceEntity>
    {
        public void Configure(EntityTypeBuilder<UserDeviceEntity> builder)
        {
            builder.ToTable("UserDevices");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.DeviceId)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.NotificationToken)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.Property(x => x.ConsumerDeviceType)
                   .IsRequired(false);

            builder.Property(x => x.IsActive)
                   .IsRequired();

            builder.Property(x => x.LastLoginDate)
                   .IsRequired(false);

            builder.Property(x => x.RoleContext)
                   .IsRequired();

            builder.Property(x => x.ActiveProfileId)
                   .IsRequired(false);
        }
    }
}