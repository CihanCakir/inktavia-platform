using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ServiceRequestAttachmentEntityConfiguration : IEntityTypeConfiguration<ServiceRequestAttachmentEntity>
{
    public void Configure(EntityTypeBuilder<ServiceRequestAttachmentEntity> builder)
    {
        builder.ToTable("service_request_attachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.HasIndex(x => x.ServiceRequestId);
        builder.HasIndex(x => x.FileId);
    }
}
