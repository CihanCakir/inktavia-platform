using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Messaging.Repository.Persistence.Configurations;

public sealed class ConversationMessageEntityConfiguration
    : IEntityTypeConfiguration<ConversationMessageEntity>
{
    public void Configure(EntityTypeBuilder<ConversationMessageEntity> builder)
    {
        builder.ToTable("conversation_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SenderName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.SenderRole).HasConversion<int>().IsRequired();
        builder.Property(x => x.Content).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.Type).HasConversion<int>().IsRequired();
        builder.Property(x => x.ModerationStatus).HasConversion<int>().IsRequired();
        builder.Property(x => x.ModerationReason).HasMaxLength(500);

        // BE_WC0 — additive parity fields. Location precision mirrors the SR source (10,7); SourceKey is the durable
        // idempotency key. All nullable — zero behaviour change until WC1/WC2.
        builder.Property(x => x.LocationLat).HasPrecision(10, 7);
        builder.Property(x => x.LocationLng).HasPrecision(10, 7);
        builder.Property(x => x.LocationLabel).HasMaxLength(500);
        builder.Property(x => x.SourceKey).HasMaxLength(200);

        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.SentAt);
        builder.HasIndex(x => x.ModerationStatus);

        // BE_WC0 — durable, multi-replica idempotency: at most one row per (conversation, source key). Partial so the
        // unconstrained native Messaging sends (null SourceKey) are unaffected. Replaces the sync consumer's in-process
        // SemaphoreSlim as the authoritative guard against the redelivery double-row race.
        builder.HasIndex(x => new { x.ConversationId, x.SourceKey })
            .IsUnique()
            .HasFilter("\"SourceKey\" IS NOT NULL")
            .HasDatabaseName("UX_conversation_messages_ConversationId_SourceKey");

        builder.HasMany(x => x.Attachments)
            .WithOne()
            .HasForeignKey("MessageId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
