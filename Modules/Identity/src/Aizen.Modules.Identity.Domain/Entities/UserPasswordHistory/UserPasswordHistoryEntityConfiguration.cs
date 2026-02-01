using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class UserPasswordHistoryEntityConfiguration : IEntityTypeConfiguration<UserPasswordHistoryEntity>
    {
        public void Configure(EntityTypeBuilder<UserPasswordHistoryEntity> builder)
        {
            builder.ToTable("UserPasswordHistories");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.PasswordHash)
                   .IsRequired();

            builder.Property(x => x.IsValid)
                   .IsRequired();

            builder.Property(x => x.ActiveProfileId)
                   .IsRequired(false);

            builder.HasOne(x => x.User)
                   .WithMany(x => x.UserPasswordHistories)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.ActiveProfile)
                   .WithMany()
                   .HasForeignKey(x => x.ActiveProfileId)
                   .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
