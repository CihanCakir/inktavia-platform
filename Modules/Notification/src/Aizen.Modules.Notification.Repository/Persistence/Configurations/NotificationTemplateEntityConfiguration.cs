using Aizen.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Notification.Repository.Persistence.Configurations;

public sealed class NotificationTemplateEntityConfiguration : IEntityTypeConfiguration<NotificationTemplateEntity>
{
    public void Configure(EntityTypeBuilder<NotificationTemplateEntity> builder)
    {
        builder.ToTable("notification_templates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TemplateCode).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.TemplateCode).IsUnique();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(1000);
        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.Channel).IsRequired();
        builder.Property(x => x.TitleTemplate).HasMaxLength(500).IsRequired();
        builder.Property(x => x.BodyTemplate).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.Type, x.Channel, x.IsActive });
    }
}
