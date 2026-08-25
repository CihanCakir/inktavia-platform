using Aizen.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Notification.Repository.Persistence.Configurations;

public sealed class NotificationCampaignEntityConfiguration : IEntityTypeConfiguration<NotificationCampaignEntity>
{
    public void Configure(EntityTypeBuilder<NotificationCampaignEntity> builder)
    {
        builder.ToTable("notification_campaigns");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Audience).IsRequired();
        builder.Property(x => x.TargetMode).IsRequired();
        builder.Property(x => x.SelectedRecipientIdsJson).HasColumnType("jsonb");
        builder.Property(x => x.TemplateCode).HasMaxLength(100);
        builder.Property(x => x.CustomContentJson).HasColumnType("jsonb");
        builder.Property(x => x.ChannelsCsv).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.TotalRecipients).IsRequired();
        builder.Property(x => x.SentCount).IsRequired();
        builder.Property(x => x.FailedCount).IsRequired();
        builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);
    }
}
