using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class VerificationDocumentEntityConfiguration : IEntityTypeConfiguration<VerificationDocumentEntity>
    {
        public void Configure(EntityTypeBuilder<VerificationDocumentEntity> builder)
        {
            builder.ToTable("VerificationDocuments");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.Property(x => x.DocumentType)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.Format)
                   .HasMaxLength(20)
                   .IsRequired(false);

            builder.Property(x => x.FileSizeDisplay)
                   .HasMaxLength(30)
                   .IsRequired(false);

            builder.Property(x => x.Issuer)
                   .HasMaxLength(300)
                   .IsRequired(false);

            builder.Property(x => x.MatchScore)
                   .HasMaxLength(20)
                   .IsRequired(false);

            builder.HasIndex(x => x.ProfileId);
            builder.HasIndex(x => x.FileId);
        }
    }
}
