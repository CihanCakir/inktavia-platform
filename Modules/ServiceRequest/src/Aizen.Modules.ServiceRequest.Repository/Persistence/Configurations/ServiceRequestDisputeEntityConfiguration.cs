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
        // BE-S13b — monetary resolution outcome (append-only; stored as int enum).
        builder.Property(x => x.ResolutionOutcome).HasConversion<int?>();
        builder.Property(x => x.ResolutionRefundAmount).HasColumnType("numeric(18,2)");
        builder.HasIndex(x => x.ServiceRequestId).IsUnique();
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.OpenedAt);
    }
}
