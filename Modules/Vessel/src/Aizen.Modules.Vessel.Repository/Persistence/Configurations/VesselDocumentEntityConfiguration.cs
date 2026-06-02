using Aizen.Modules.Vessel.Domain.Entities.Vessel;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Vessel.Repository.Persistence.Configurations;

public sealed class VesselDocumentEntityConfiguration : IEntityTypeConfiguration<VesselDocumentEntity>
{
    public void Configure(EntityTypeBuilder<VesselDocumentEntity> builder)
    {
        builder.ToTable("vessel_documents", "vessel");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.VesselId).IsRequired();
        builder.Property(x => x.DocumentTypeCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.DocumentName).HasMaxLength(250).IsRequired();
        builder.Property(x => x.FileId);
        builder.Property(x => x.OriginalFileNameSnapshot).HasMaxLength(500);
        builder.Property(x => x.ContentTypeSnapshot).HasMaxLength(200);
        builder.Property(x => x.SizeInBytesSnapshot);
        builder.Property(x => x.DocumentStatus).IsRequired();
        builder.Property(x => x.ExpiresAt);
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => new { x.VesselId, x.DocumentTypeCode });
        builder.HasIndex(x => x.ExpiresAt);

        builder.HasOne(x => x.Vessel)
            .WithMany(x => x.Documents)
            .HasForeignKey(x => x.VesselId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

