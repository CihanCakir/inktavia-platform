using Aizen.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Notification.Repository.Persistence.Configurations;

public sealed class NotificationTemplateContentEntityConfiguration
    : IEntityTypeConfiguration<NotificationTemplateContentEntity>
{
    public void Configure(EntityTypeBuilder<NotificationTemplateContentEntity> builder)
    {
        builder.ToTable("notification_template_contents");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TemplateId).IsRequired();
        builder.Property(x => x.Channel).IsRequired();
        builder.Property(x => x.Locale).HasMaxLength(8).IsRequired();
        builder.Property(x => x.Version).IsRequired();
        builder.Property(x => x.Status).IsRequired();

        // Kanal-özel içerik kolonları — hepsi nullable (tek tablo, az nullable kolon kararı; entity'deki nota bkz.).
        builder.Property(x => x.SubjectTemplate).HasMaxLength(500);
        builder.Property(x => x.HtmlTemplate).HasMaxLength(20000);
        builder.Property(x => x.TextTemplate).HasMaxLength(8000);
        builder.Property(x => x.TitleTemplate).HasMaxLength(500);
        builder.Property(x => x.BodyTemplate).HasMaxLength(4000);
        builder.Property(x => x.DeepLinkTemplate).HasMaxLength(1000);
        builder.Property(x => x.SmsTextTemplate).HasMaxLength(1000);
        builder.Property(x => x.LayoutCode).HasMaxLength(50);

        builder.Property(x => x.CreatedAt).IsRequired();

        // Bir template'in her (kanal, locale, version) kombinasyonu benzersiz.
        builder.HasIndex(x => new { x.TemplateId, x.Channel, x.Locale, x.Version }).IsUnique();
        // Renderer'ın Published-seçim sorgusu için yardımcı indeks.
        builder.HasIndex(x => new { x.TemplateId, x.Channel, x.Status });

        // FK → notification_templates (mantıksal template silinirse içerikleri de gider).
        builder.HasOne<NotificationTemplateEntity>()
               .WithMany()
               .HasForeignKey(x => x.TemplateId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
