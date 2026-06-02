using Aizen.Modules.FileStorage.Domain.Entities.Processing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.FileStorage.Repository.Persistence.Configurations;

public sealed class FileProcessingJobEntityConfiguration : IEntityTypeConfiguration<FileProcessingJobEntity>
{
    public void Configure(EntityTypeBuilder<FileProcessingJobEntity> builder)
    {
        builder.ToTable("file_processing_jobs", "file_storage");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ErrorCode).HasMaxLength(100);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);
        builder.Property(x => x.ResultDocumentId).HasMaxLength(200);

        builder.Property(x => x.ProcessingType).HasConversion<int>().IsRequired();
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();

        builder.HasIndex(x => new { x.FileId, x.ProcessingType });
        builder.HasIndex(x => x.Status);

        builder.HasOne(x => x.File)
            .WithMany(f => f.ProcessingJobs)
            .HasForeignKey(x => x.FileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
