using Aizen.Modules.CargoDry.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.CargoDry.Repository.Persistence.Configurations;

public sealed class CargoDryKitLifecycleEventEntityConfiguration
    : IEntityTypeConfiguration<CargoDryKitLifecycleEventEntity>
{
    public void Configure(EntityTypeBuilder<CargoDryKitLifecycleEventEntity> builder)
    {
        builder.ToTable("kit_lifecycle_events");
        builder.HasKey(x => x.Id);

        // Kit identity
        builder.Property(x => x.KitId).IsRequired();
        builder.Property(x => x.KitCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.SerialNumber).HasMaxLength(100);
        builder.Property(x => x.BatchCode).HasMaxLength(50);
        builder.Property(x => x.ProductCode).HasMaxLength(50);

        // Event
        builder.Property(x => x.EventType).HasConversion<int>().IsRequired();
        builder.Property(x => x.PreviousStatus).HasMaxLength(50);
        builder.Property(x => x.NewStatus).HasMaxLength(50);

        // Actor
        builder.Property(x => x.ActorUserId);
        builder.Property(x => x.ActorType).HasMaxLength(50);

        // Context
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.ReferenceId);
        builder.Property(x => x.ReferenceType).HasMaxLength(100);
        builder.Property(x => x.MetadataJson).HasColumnType("text");

        // Timestamp
        builder.Property(x => x.OccurredAtUtc).IsRequired();

        // Indexes for common query patterns
        builder.HasIndex(x => x.KitId);
        builder.HasIndex(x => x.KitCode);
        builder.HasIndex(x => x.BatchCode);
        builder.HasIndex(x => x.ProductCode);
        builder.HasIndex(x => x.EventType);
        builder.HasIndex(x => x.ActorUserId);
        builder.HasIndex(x => x.OccurredAtUtc);
    }
}
