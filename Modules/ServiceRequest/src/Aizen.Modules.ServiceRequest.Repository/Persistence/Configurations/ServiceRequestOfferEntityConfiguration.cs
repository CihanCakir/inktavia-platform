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

        // Totals (computed by calculation service)
        builder.Property(x => x.Subtotal).HasPrecision(18, 2);
        builder.Property(x => x.TaxTotal).HasPrecision(18, 2);
        builder.Property(x => x.DiscountTotal).HasPrecision(18, 2);
        builder.Property(x => x.GrandTotal).HasPrecision(18, 2);

        // Per-category totals
        builder.Property(x => x.ServiceTotal).HasPrecision(18, 2);
        builder.Property(x => x.ProductTotal).HasPrecision(18, 2);
        builder.Property(x => x.LaborTotal).HasPrecision(18, 2);
        builder.Property(x => x.InstallationTotal).HasPrecision(18, 2);
        builder.Property(x => x.InspectionTotal).HasPrecision(18, 2);
        builder.Property(x => x.DeliveryTotal).HasPrecision(18, 2);
        builder.Property(x => x.EmergencyFeeTotal).HasPrecision(18, 2);
        builder.Property(x => x.OtherTotal).HasPrecision(18, 2);

        // Commercial terms
        builder.Property(x => x.DepositValue).HasPrecision(18, 2);
        builder.Property(x => x.PaymentTermsNote).HasMaxLength(2000);
        builder.Property(x => x.WarrantyNote).HasMaxLength(2000);

        // Concurrency token — PostgreSQL xmin
        builder.UseXminAsConcurrencyToken();

        builder.HasIndex(x => x.ServiceRequestId);
        builder.HasIndex(x => x.ProviderProfileId);
        builder.HasIndex(x => x.Status);
        builder.HasMany(x => x.Items).WithOne(x => x.Offer).HasForeignKey(x => x.ServiceRequestOfferId).OnDelete(DeleteBehavior.Cascade);
    }
}
