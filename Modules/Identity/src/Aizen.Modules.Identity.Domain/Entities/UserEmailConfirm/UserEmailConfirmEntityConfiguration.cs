using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class UserEmailConfirmEntityConfiguration : IEntityTypeConfiguration<UserEmailConfirmEntity>
    {
        public void Configure(EntityTypeBuilder<UserEmailConfirmEntity> builder)
        {
            builder.ToTable("UserEmailConfirms");
            builder.HasKey(u => u.Id);
            builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
            builder.Property(u => u.ValidationGuid).IsRequired().HasMaxLength(36);
            builder.Property(u => u.ValidationString).IsRequired().HasMaxLength(256);
        }
    }
    public static class UserEmailConfirmEntityConfigurationExtensions
    {
        public static void AddUserEmailConfirmEntityConfiguration(this ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserEmailConfirmEntityConfiguration());
        }
    }
}