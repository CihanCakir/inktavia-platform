using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ServiceRequestStatusHistoryEntityConfiguration : IEntityTypeConfiguration<ServiceRequestStatusHistoryEntity>
{
    public void Configure(EntityTypeBuilder<ServiceRequestStatusHistoryEntity> builder)
    {
        builder.ToTable("service_request_status_histories");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.HasIndex(x => x.ServiceRequestId);
        builder.HasIndex(x => x.OccurredAt);
    }
}
