using Aizen.Modules.ServiceRequest.Domain.Entities.WorkPhase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class WorkPhaseEntityConfiguration : IEntityTypeConfiguration<WorkPhaseEntity>
{
    public void Configure(EntityTypeBuilder<WorkPhaseEntity> builder)
    {
        builder.ToTable("service_request_work_phases");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(50);
        builder.HasIndex(x => x.ServiceRequestId);
    }
}
