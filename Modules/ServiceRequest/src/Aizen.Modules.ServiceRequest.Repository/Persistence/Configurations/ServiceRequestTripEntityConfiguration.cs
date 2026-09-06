using Aizen.Modules.ServiceRequest.Domain.Entities.Trip;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ServiceRequestTripEntityConfiguration : IEntityTypeConfiguration<ServiceRequestTripEntity>
{
    public void Configure(EntityTypeBuilder<ServiceRequestTripEntity> builder)
    {
        builder.ToTable("service_request_trips");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ServiceRequestId).IsRequired();
        builder.Property(x => x.ProviderProfileId).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.StartedAt).IsRequired();
        builder.Property(x => x.ArrivedAt);
        builder.Property(x => x.CancelledAt);
        builder.Property(x => x.LastLatitude).HasPrecision(10, 7);
        builder.Property(x => x.LastLongitude).HasPrecision(10, 7);
        builder.Property(x => x.LastHeading).HasPrecision(5, 2);
        builder.Property(x => x.LastPingAt);
        builder.Property(x => x.TotalDistanceKm).HasPrecision(12, 3);
        builder.Property(x => x.DurationSeconds);

        // One trip row per SR (re-armed on a fresh en-route). Enforces "one active trip per SR".
        builder.HasIndex(x => x.ServiceRequestId).IsUnique();
        builder.HasIndex(x => x.ProviderProfileId);
        builder.HasIndex(x => x.Status);
    }
}
