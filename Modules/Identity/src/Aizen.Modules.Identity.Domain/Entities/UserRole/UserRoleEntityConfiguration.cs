using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class UserRoleEntityConfiguration : IEntityTypeConfiguration<UserRoleEntity>
    {
        public void Configure(EntityTypeBuilder<UserRoleEntity> builder)
        {
            builder.ToTable("UserRoles");



        builder.HasKey(x => new { x.UserId, x.RoleId });


           builder.Property(x => x.UserId)
                   .IsRequired();

            builder.Property(x => x.RoleId)
                   .IsRequired();

            builder.HasOne(x => x.User)
                   .WithMany(x => x.UserRoles)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Role)
                   .WithMany()
                   .HasForeignKey(x => x.RoleId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
