using Aizen.Modules.ServiceRequest.Domain.Entities.WorkLog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ServiceRequestWorkLogEntityConfiguration : IEntityTypeConfiguration<ServiceRequestWorkLogEntity>
{
    public void Configure(EntityTypeBuilder<ServiceRequestWorkLogEntity> builder)
    {
        builder.ToTable("service_request_work_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.LocationLatitude).HasPrecision(10, 7);
        builder.Property(x => x.LocationLongitude).HasPrecision(10, 7);
        builder.HasIndex(x => x.ServiceRequestId);
        builder.HasIndex(x => x.ServiceRequestAssignmentId);
        builder.HasIndex(x => x.LoggedAt);
    }
}
