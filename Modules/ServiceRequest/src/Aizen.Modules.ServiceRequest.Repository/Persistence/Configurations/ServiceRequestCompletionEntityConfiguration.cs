using Aizen.Modules.ServiceRequest.Domain.Entities.Completion;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ServiceRequestCompletionEntityConfiguration : IEntityTypeConfiguration<ServiceRequestCompletionEntity>
{
    public void Configure(EntityTypeBuilder<ServiceRequestCompletionEntity> builder)
    {
        builder.ToTable("service_request_completions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CompletionNotes).HasMaxLength(4000);
        builder.Property(x => x.ReviewNotes).HasMaxLength(2000);
        builder.Property(x => x.ClientRating);
        builder.HasIndex(x => x.ServiceRequestId).IsUnique();
        builder.HasIndex(x => x.Status);
        // N3-C — the auto-approval job scans (Status, AutoApproveAt); index the deadline for the range read.
        builder.HasIndex(x => x.AutoApproveAt);
    }
}
