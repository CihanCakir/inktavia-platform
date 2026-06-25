using Aizen.Modules.ServiceRequest.Domain.Entities.Assignment;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ServiceRequestAssignmentEntityConfiguration : IEntityTypeConfiguration<ServiceRequestAssignmentEntity>
{
    public void Configure(EntityTypeBuilder<ServiceRequestAssignmentEntity> builder)
    {
        builder.ToTable("service_request_assignments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ProviderNotes).HasMaxLength(2000);
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.Property(x => x.CancellationReason).HasMaxLength(1000);
        builder.HasIndex(x => x.ServiceRequestId).IsUnique();
        builder.HasIndex(x => x.ProviderProfileId);
        builder.HasIndex(x => x.Status);
        builder.HasMany(x => x.WorkLogs).WithOne().HasForeignKey(x => x.ServiceRequestAssignmentId).OnDelete(DeleteBehavior.Cascade);
    }
}
