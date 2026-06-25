using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ServiceRequestDisputeEntityConfiguration : IEntityTypeConfiguration<ServiceRequestDisputeEntity>
{
    public void Configure(EntityTypeBuilder<ServiceRequestDisputeEntity> builder)
    {
        builder.ToTable("service_request_disputes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.ResolutionNotes).HasMaxLength(4000);
        builder.HasIndex(x => x.ServiceRequestId).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.OpenedAt);
    }
}
