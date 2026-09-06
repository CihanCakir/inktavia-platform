using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class UserProfileEntityConfiguration : IEntityTypeConfiguration<UserProfileEntity>
    {
        public void Configure(EntityTypeBuilder<UserProfileEntity> builder)
        {
            builder.ToTable("UserProfiles");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.FirstName)
                   .IsRequired();

            builder.Property(x => x.LastName)
                   .IsRequired();

            builder.Property(x => x.Gender)
                   .IsRequired(false);

            builder.Property(x => x.BirthDate)
                   .IsRequired(false);

            builder.Property(x => x.Bio)
                   .IsRequired(false);

            builder.Property(x => x.ProfilePhotoUrl)
                   .IsRequired(false);

            builder.Property(x => x.CompanyName)
                   .HasMaxLength(300)
                   .IsRequired(false);

            builder.Property(x => x.City)
                   .HasMaxLength(100)
                   .IsRequired(false);

            builder.Property(x => x.Country)
                   .HasMaxLength(100)
                   .IsRequired(false);

            builder.Property(x => x.BusinessLatitude)
                   .HasPrecision(10, 7)
                   .IsRequired(false);

            builder.Property(x => x.BusinessLongitude)
                   .HasPrecision(10, 7)
                   .IsRequired(false);

            builder.Property(x => x.BusinessAddressLabel)
                   .HasMaxLength(300)
                   .IsRequired(false);

            builder.Property(x => x.RatePerKm)
                   .HasPrecision(12, 4)
                   .IsRequired(false);

            builder.Property(x => x.RejectionCategory)
                   .HasMaxLength(100)
                   .IsRequired(false);

            builder.Property(x => x.InternalNote)
                   .HasMaxLength(2000)
                   .IsRequired(false);

            builder.Property(x => x.ReviewedBy)
                   .HasMaxLength(256)
                   .IsRequired(false);

            builder.Property(x => x.ReviewedAt)
                   .IsRequired(false);

            builder.HasOne(x => x.User)
                   .WithMany(x => x.Profiles)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.VerificationDocuments)
                   .WithOne()
                   .HasForeignKey(x => x.ProfileId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.RiskSignals)
                   .WithOne()
                   .HasForeignKey(x => x.ProfileId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
