using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class RiskSignalEntityConfiguration : IEntityTypeConfiguration<RiskSignalEntity>
    {
        public void Configure(EntityTypeBuilder<RiskSignalEntity> builder)
        {
            builder.ToTable("RiskSignals");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Severity)
                   .HasMaxLength(20)
                   .IsRequired();

            builder.Property(x => x.Title)
                   .HasMaxLength(200)
                   .IsRequired();

            builder.Property(x => x.Description)
                   .HasMaxLength(1000)
                   .IsRequired();

            builder.Property(x => x.SignalCode)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.HasIndex(x => x.ProfileId);
        }
    }
}
