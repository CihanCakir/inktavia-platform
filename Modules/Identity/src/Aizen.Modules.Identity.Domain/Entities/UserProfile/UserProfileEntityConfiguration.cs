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

            builder.HasOne(x => x.User)
                   .WithMany(x => x.Profiles)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
