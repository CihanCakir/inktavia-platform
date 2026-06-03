using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ServiceRequestItemEntityConfiguration : IEntityTypeConfiguration<ServiceRequestItemEntity>
{
    public void Configure(EntityTypeBuilder<ServiceRequestItemEntity> builder)
    {
        builder.ToTable("service_request_items");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(2000);
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.UnitCode).HasMaxLength(50);
        builder.Property(x => x.EstimatedUnitPrice).HasPrecision(18, 4);
        builder.HasIndex(x => x.ServiceRequestId);
    }
}
