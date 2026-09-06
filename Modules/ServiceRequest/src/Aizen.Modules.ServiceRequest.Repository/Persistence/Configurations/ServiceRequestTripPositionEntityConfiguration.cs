using Aizen.Modules.ServiceRequest.Domain.Entities.Trip;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ServiceRequestTripPositionEntityConfiguration : IEntityTypeConfiguration<ServiceRequestTripPositionEntity>
{
    public void Configure(EntityTypeBuilder<ServiceRequestTripPositionEntity> builder)
    {
        builder.ToTable("trip_positions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TripId).IsRequired();
        builder.Property(x => x.Latitude).HasPrecision(10, 7).IsRequired();
        builder.Property(x => x.Longitude).HasPrecision(10, 7).IsRequired();
        builder.Property(x => x.Heading).HasPrecision(5, 2);
        builder.Property(x => x.PingAt).IsRequired();

        builder.HasIndex(x => new { x.TripId, x.PingAt });
    }
}
