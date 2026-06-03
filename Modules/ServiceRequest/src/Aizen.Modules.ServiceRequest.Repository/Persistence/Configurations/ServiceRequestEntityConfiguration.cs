using Aizen.Modules.ServiceRequest.Domain.Entities.Assignment;
using Aizen.Modules.ServiceRequest.Domain.Entities.Completion;
using Aizen.Modules.ServiceRequest.Domain.Entities.Dispute;
using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Aizen.Modules.ServiceRequest.Domain.Entities.ServiceRequest;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ServiceRequestEntityConfiguration : IEntityTypeConfiguration<ServiceRequestEntity>
{
    public void Configure(EntityTypeBuilder<ServiceRequestEntity> builder)
    {
        builder.ToTable("service_requests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RequestCode).IsRequired().HasMaxLength(50);
        builder.Property(x => x.OwnerUserId).IsRequired();
        builder.Property(x => x.VesselId).IsRequired();
        builder.Property(x => x.ServiceCategoryCode).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ServiceTypeCode).HasMaxLength(100);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.Priority).IsRequired();
        builder.Property(x => x.LocationCountryCode).HasMaxLength(10);
        builder.Property(x => x.LocationCityCode).HasMaxLength(50);
        builder.Property(x => x.LocationMarinaName).HasMaxLength(200);
        builder.Property(x => x.LocationLatitude).HasPrecision(10, 7);
        builder.Property(x => x.LocationLongitude).HasPrecision(10, 7);
        builder.Property(x => x.OwnerNotes).HasMaxLength(2000);
        builder.Property(x => x.CancelReason).HasMaxLength(1000);
        builder.HasIndex(x => x.RequestCode).IsUnique();
        builder.HasIndex(x => x.OwnerUserId);
        builder.HasIndex(x => x.VesselId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreateDate);

        builder.HasMany(x => x.Items).WithOne(x => x.ServiceRequest).HasForeignKey(x => x.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.StatusHistory).WithOne(x => x.ServiceRequest).HasForeignKey(x => x.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Attachments).WithOne(x => x.ServiceRequest).HasForeignKey(x => x.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Messages).WithOne(x => x.ServiceRequest).HasForeignKey(x => x.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Offers).WithOne().HasForeignKey(x => x.ServiceRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Assignment).WithOne().HasForeignKey<ServiceRequestAssignmentEntity>(x => x.ServiceRequestId);
        builder.HasOne(x => x.Completion).WithOne().HasForeignKey<ServiceRequestCompletionEntity>(x => x.ServiceRequestId);
        builder.HasOne(x => x.Dispute).WithOne().HasForeignKey<ServiceRequestDisputeEntity>(x => x.ServiceRequestId);
    }
}
