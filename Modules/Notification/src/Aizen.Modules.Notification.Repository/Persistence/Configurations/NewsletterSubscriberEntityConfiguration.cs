using Aizen.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Notification.Repository.Persistence.Configurations;

public sealed class NewsletterSubscriberEntityConfiguration : IEntityTypeConfiguration<NewsletterSubscriberEntity>
{
    public void Configure(EntityTypeBuilder<NewsletterSubscriberEntity> builder)
    {
        builder.ToTable("newsletter_subscribers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Email).HasMaxLength(320).IsRequired();
        builder.Property(x => x.Lang).HasMaxLength(10).IsRequired();
        builder.Property(x => x.SourcePage).HasMaxLength(500);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.ConfirmationToken).HasMaxLength(64);
        builder.Property(x => x.ConfirmationTokenExpiresAt);
        builder.Property(x => x.ConfirmedAt);
        builder.Property(x => x.UnsubscribedAt);
        builder.Property(x => x.UnsubscribeToken).HasMaxLength(64).IsRequired();
        builder.Property(x => x.SpamScore).IsRequired();
        builder.Property(x => x.IpHash).HasMaxLength(64);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();

        builder.HasIndex(x => x.Email).IsUnique();
        builder.HasIndex(x => x.ConfirmationToken);              // single-use redemption lookup
        builder.HasIndex(x => x.UnsubscribeToken).IsUnique();    // one-click unsubscribe lookup
        builder.HasIndex(x => new { x.IpHash, x.CreatedAt });    // rate/abuse correlation
        builder.HasIndex(x => new { x.Status, x.CreatedAt });    // admin list / stats
    }
}
