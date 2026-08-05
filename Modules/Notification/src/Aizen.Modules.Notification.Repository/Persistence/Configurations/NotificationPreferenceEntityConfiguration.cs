using Aizen.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Notification.Repository.Persistence.Configurations;

public sealed class NotificationPreferenceEntityConfiguration : IEntityTypeConfiguration<NotificationPreferenceEntity>
{
    public void Configure(EntityTypeBuilder<NotificationPreferenceEntity> builder)
    {
        builder.ToTable("notification_preferences");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.Category).IsRequired();
        builder.Property(x => x.Channel).IsRequired();
        builder.Property(x => x.Enabled).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        // One row per (user, category, channel).
        builder.HasIndex(x => new { x.UserId, x.Category, x.Channel }).IsUnique();
    }
}
