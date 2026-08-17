using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryRenewalPreparationEntityConfiguration
    : IEntityTypeConfiguration<CargoDryRenewalPreparationEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryRenewalPreparationEntity> builder)
    {
        builder.ToTable("renewal_preparations");
        builder.HasKey(x => x.Id);

        // Identity
        builder.Property(x => x.RenewalCode).HasMaxLength(64).IsRequired();
        builder.HasIndex(x => x.RenewalCode).IsUnique();

        // Kit context
        builder.Property(x => x.KitId).IsRequired();
        builder.Property(x => x.KitCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.ProductCode).HasMaxLength(50).IsRequired();

        // Owner / vessel / provider (Id-only, no EF FK)
        builder.Property(x => x.OwnerUserId);
        builder.Property(x => x.VesselId);
        builder.Property(x => x.ProviderProfileId);

        // Renewal parameters
        builder.Property(x => x.CurrentExpiresAtUtc).IsRequired();
        builder.Property(x => x.RequestedRenewalMonths).IsRequired();
        builder.Property(x => x.NewExpiresAtUtc);
        builder.Property(x => x.RenewalPrice).HasPrecision(18, 4).IsRequired();
        builder.Property(x => x.CurrencyCode).HasMaxLength(10).IsRequired();

        // Status
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();

        // Payment / invoice (cross-module Id-only)
        builder.Property(x => x.InvoiceId);
        builder.Property(x => x.PaymentTransactionId);
        builder.Property(x => x.ManualPaymentReference).HasMaxLength(200);

        // Notification tracking
        builder.Property(x => x.NotificationCorrelationId).HasMaxLength(100);
        builder.Property(x => x.NotificationStatus).HasConversion<int>().IsRequired();
        builder.Property(x => x.NotificationChannels).HasMaxLength(200);          // JSON array string
        builder.Property(x => x.LastNotificationTemplateCode).HasMaxLength(100);
        builder.Property(x => x.LastNotificationLanguageCode).HasMaxLength(10);
        builder.Property(x => x.NotificationPreparedAtUtc);
        builder.Property(x => x.NotificationPreparedByUserId);
        builder.Property(x => x.NotificationDispatchedAtUtc);
        builder.Property(x => x.NotificationDispatchedByUserId);
        builder.Property(x => x.NotificationFailureReason).HasMaxLength(1000);

        // Lifecycle audit
        builder.Property(x => x.PreparedByUserId);
        builder.Property(x => x.PreparedAtUtc).IsRequired();
        builder.Property(x => x.CompletedByUserId);
        builder.Property(x => x.CompletedAtUtc);
        builder.Property(x => x.CancelledByUserId);
        builder.Property(x => x.CancelledAtUtc);
        builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.Note).HasMaxLength(1000);

        // Query indexes
        builder.HasIndex(x => x.KitId);
        builder.HasIndex(x => x.KitCode);
        builder.HasIndex(x => x.ProductCode);
        builder.HasIndex(x => x.OwnerUserId);
        builder.HasIndex(x => x.VesselId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.PreparedAtUtc);

        // Composite: enforce fast "open prep per kit" query
        builder.HasIndex(x => new { x.KitId, x.Status });
    }
}
