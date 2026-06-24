using Aizen.Modules.ServiceRequest.Domain.Entities.Conversation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ConversationMessageEntityConfiguration : IEntityTypeConfiguration<ConversationMessageEntity>
{
    public void Configure(EntityTypeBuilder<ConversationMessageEntity> builder)
    {
        builder.ToTable("conversation_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SenderId).IsRequired().HasMaxLength(200);
        builder.Property(x => x.SenderName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.SenderRole).IsRequired().HasMaxLength(50);
        builder.Property(x => x.Content).IsRequired().HasMaxLength(4000);
        builder.HasIndex(x => x.ConversationId);
        builder.HasMany(x => x.Attachments).WithOne().HasForeignKey("MessageId").OnDelete(DeleteBehavior.Cascade);
    }
}
