using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class RoleTypeEntityConfiguration : IEntityTypeConfiguration<RoleTypeEntity>   
    {
        public void Configure(EntityTypeBuilder<RoleTypeEntity> builder)
        {
            builder.ToTable("RoleTypes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                   .HasMaxLength(100)
                   .IsRequired();

            builder.Property(x => x.Description)
                   .HasMaxLength(500)
                   .IsRequired(false);

            builder.HasMany(x => x.Roles)
                   .WithOne(x => x.RoleType)
                   .HasForeignKey(x => x.RoleTypeId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }

    public static class RoleTypeEntityConfigurationExtensions
    {
        public static void AddRoleTypeConfiguration(this ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new RoleTypeEntityConfiguration());
        }

    }
}