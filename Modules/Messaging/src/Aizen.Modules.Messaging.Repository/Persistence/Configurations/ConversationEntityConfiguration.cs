using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Messaging.Repository.Persistence.Configurations;

public sealed class ConversationEntityConfiguration : IEntityTypeConfiguration<ConversationEntity>
{
    public void Configure(EntityTypeBuilder<ConversationEntity> builder)
    {
        builder.ToTable("conversations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.ContextType).HasConversion<int>().IsRequired();
        builder.Property(x => x.Topic).HasConversion<int>();   // N-D: nullable support topic (null for non-support)
        builder.Property(x => x.LastMessagePreview).HasMaxLength(200);

        builder.HasIndex(x => x.ContextType);
        builder.HasIndex(x => x.Topic);
        builder.HasIndex(x => new { x.ContextType, x.ContextId }).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.LastMessageAt);

        builder.HasMany(x => x.Participants)
            .WithOne()
            .HasForeignKey("ConversationId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Messages)
            .WithOne()
            .HasForeignKey("ConversationId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
