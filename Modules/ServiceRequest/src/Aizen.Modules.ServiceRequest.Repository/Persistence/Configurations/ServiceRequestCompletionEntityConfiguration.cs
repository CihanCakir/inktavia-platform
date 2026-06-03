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
        builder.HasIndex(x => x.ServiceRequestId).IsUnique();
        builder.HasIndex(x => x.Status);
    }
}
