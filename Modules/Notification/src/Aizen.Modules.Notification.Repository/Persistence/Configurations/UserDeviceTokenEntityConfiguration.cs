using Aizen.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Notification.Repository.Persistence.Configurations;

public sealed class UserDeviceTokenEntityConfiguration : IEntityTypeConfiguration<UserDeviceTokenEntity>
{
    public void Configure(EntityTypeBuilder<UserDeviceTokenEntity> builder)
    {
        builder.ToTable("user_device_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.DeviceToken).HasMaxLength(512).IsRequired();
        builder.HasIndex(x => x.DeviceToken).IsUnique();
        builder.Property(x => x.Platform).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RegisteredAt).IsRequired();
        builder.Property(x => x.LastActiveAt).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.IsActive });
    }
}
