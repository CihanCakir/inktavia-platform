using Aizen.Modules.ServiceRequest.Domain.Entities.Conversation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class MessageAttachmentEntityConfiguration : IEntityTypeConfiguration<MessageAttachmentEntity>
{
    public void Configure(EntityTypeBuilder<MessageAttachmentEntity> builder)
    {
        builder.ToTable("message_attachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Type).IsRequired().HasMaxLength(50);
        builder.Property(x => x.FileId).HasMaxLength(200);
        builder.HasIndex(x => x.MessageId);
    }
}
