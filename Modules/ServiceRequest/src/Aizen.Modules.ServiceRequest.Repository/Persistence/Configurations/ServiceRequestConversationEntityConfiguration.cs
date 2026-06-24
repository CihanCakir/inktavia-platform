using Aizen.Modules.ServiceRequest.Domain.Entities.Conversation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ServiceRequestConversationEntityConfiguration : IEntityTypeConfiguration<ServiceRequestConversationEntity>
{
    public void Configure(EntityTypeBuilder<ServiceRequestConversationEntity> builder)
    {
        builder.ToTable("sr_conversations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.ServiceRequestId);
        builder.HasMany(x => x.Messages).WithOne().HasForeignKey("ConversationId").OnDelete(DeleteBehavior.Cascade);
    }
}
