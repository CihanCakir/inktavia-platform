using Aizen.Modules.ServiceRequest.Domain.Entities.ChangeOrder;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.ServiceRequest.Repository.Persistence.Configurations;

public sealed class ServiceChangeOrderEntityConfiguration : IEntityTypeConfiguration<ServiceChangeOrderEntity>
{
    public void Configure(EntityTypeBuilder<ServiceChangeOrderEntity> builder)
    {
        builder.ToTable("service_change_orders");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Status).IsRequired().HasConversion<int>();
        builder.Property(x => x.Direction).IsRequired().HasConversion<int>();
        builder.Property(x => x.CurrencyCode).IsRequired().HasMaxLength(10);
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(2000);
        builder.Property(x => x.RejectionReason).HasMaxLength(2000);

        builder.Property(x => x.AppliedCustomerTotal).HasPrecision(18, 2).IsRequired().HasDefaultValue(0m);
        builder.Property(x => x.AppliedProviderNet).HasPrecision(18, 2).IsRequired().HasDefaultValue(0m);

        builder.Ignore(x => x.EffectiveTotalDelta);

        builder.UseXminAsConcurrencyToken();

        builder.HasIndex(x => x.ServiceRequestId);
        builder.HasIndex(x => x.AcceptedOfferId);
        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.ChangeOrder)
            .HasForeignKey(x => x.ServiceChangeOrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
