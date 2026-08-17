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

        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.SentAt);
        builder.HasIndex(x => x.ModerationStatus);

        builder.HasMany(x => x.Attachments)
            .WithOne()
            .HasForeignKey("MessageId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
