using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ServiceRequestMessageEntityConfiguration : IEntityTypeConfiguration<ServiceRequestMessageEntity>
{
    public void Configure(EntityTypeBuilder<ServiceRequestMessageEntity> builder)
    {
        builder.ToTable("service_request_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Content).IsRequired().HasMaxLength(5000);
        builder.HasIndex(x => x.ServiceRequestId);
        builder.HasIndex(x => x.SenderUserId);
        builder.HasIndex(x => new { x.ServiceRequestId, x.IsRead });
        builder.Property(x => x.LocationLat).HasPrecision(10, 7);
        builder.Property(x => x.LocationLng).HasPrecision(10, 7);
        builder.Property(x => x.LocationLabel).HasMaxLength(500);
    }
}
