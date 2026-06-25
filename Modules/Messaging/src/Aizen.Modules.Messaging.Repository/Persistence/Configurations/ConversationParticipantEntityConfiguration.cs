using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Messaging.Repository.Persistence.Configurations;

public sealed class ConversationParticipantEntityConfiguration
    : IEntityTypeConfiguration<ConversationParticipantEntity>
{
    public void Configure(EntityTypeBuilder<ConversationParticipantEntity> builder)
    {
        builder.ToTable("conversation_participants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Role).HasConversion<int>().IsRequired();
        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => new { x.ConversationId, x.UserId });
    }
}
