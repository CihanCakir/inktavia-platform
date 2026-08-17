using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Messaging.Repository.Persistence.Configurations;

public sealed class MessageAttachmentEntityConfiguration
    : IEntityTypeConfiguration<MessageAttachmentEntity>
{
    public void Configure(EntityTypeBuilder<MessageAttachmentEntity> builder)
    {
        builder.ToTable("message_attachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.FileType).HasMaxLength(50);
        builder.Property(x => x.FileStorageId).HasMaxLength(200);
        builder.HasIndex(x => x.MessageId);
    }
}
