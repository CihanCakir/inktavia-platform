using Aizen.Modules.ServiceRequest.Domain.Entities.Offer;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ServiceRequestOfferEntityConfiguration : IEntityTypeConfiguration<ServiceRequestOfferEntity>
{
    public void Configure(EntityTypeBuilder<ServiceRequestOfferEntity> builder)
    {
        builder.ToTable("service_request_offers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.TotalAmount).IsRequired().HasPrecision(18, 4);
        builder.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.ProviderNotes).HasMaxLength(2000);
        builder.Property(x => x.RejectionReason).HasMaxLength(1000);
        builder.Property(x => x.WithdrawalReason).HasMaxLength(1000);
        builder.HasIndex(x => x.ServiceRequestId);
        builder.HasIndex(x => x.ProviderProfileId);
        builder.HasIndex(x => x.Status);
        builder.HasMany(x => x.Items).WithOne(x => x.Offer).HasForeignKey(x => x.ServiceRequestOfferId).OnDelete(DeleteBehavior.Cascade);
    }
}
