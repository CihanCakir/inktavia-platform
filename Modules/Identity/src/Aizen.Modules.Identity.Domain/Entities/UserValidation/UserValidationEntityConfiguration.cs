using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Identity.Domain.Entities.UserValidation
{
    public class UserValidationEntityConfiguration : IEntityTypeConfiguration<UserValidationEntity>
    {
        public void Configure(EntityTypeBuilder<UserValidationEntity> builder)
        {
            builder.ToTable("user_validations");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ValidationReferance)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.ValidationGuid)
                   .IsRequired()
                   .HasMaxLength(100);

            builder.Property(x => x.ValdationCode).IsRequired();
            builder.Property(x => x.State).IsRequired();
            builder.Property(x => x.ApplicationId).IsRequired();

            builder.HasOne(x => x.UserProfile)
                   .WithMany()
                   .HasForeignKey(x => x.UserProfileId)
                   .IsRequired(false)
                   .OnDelete(DeleteBehavior.SetNull);
        }
    }

}