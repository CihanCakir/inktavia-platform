using Aizen.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Notification.Repository.Persistence.Configurations;

public sealed class NotificationEntityConfiguration : IEntityTypeConfiguration<NotificationEntity>
{
    public void Configure(EntityTypeBuilder<NotificationEntity> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RecipientUserId).IsRequired();
        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.Channel).IsRequired();
        builder.Property(x => x.TemplateCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.DeliveryProviderRef).HasMaxLength(255);
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb");
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.RecipientUserId, x.Status });
        builder.HasIndex(x => new { x.RecipientUserId, x.CreatedAt });
    }
}
