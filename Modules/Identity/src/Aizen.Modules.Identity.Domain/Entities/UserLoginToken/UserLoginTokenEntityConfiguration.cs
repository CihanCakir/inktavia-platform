using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Identity.Domain.Entities
{
    public class UserLoginTokenEntityConfiguration : IEntityTypeConfiguration<UserLoginTokenEntity>
    {
        public void Configure(EntityTypeBuilder<UserLoginTokenEntity> builder)
        {
            builder.ToTable("UserLoginTokens");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.AccessToken)
                   .IsRequired();

            builder.Property(x => x.RefreshToken)
                   .IsRequired();

            builder.Property(x => x.AccessTokenExpiration)
                   .IsRequired();

            builder.Property(x => x.RefreshTokenExpiration)
                   .IsRequired();

            builder.Property(x => x.IsRevoked)
                   .IsRequired();

            builder.Property(x => x.DeviceId)
                   .IsRequired();

            builder.Property(x => x.RoleContext)
                   .IsRequired();

            builder.Property(x => x.ActiveProfileId)
                   .IsRequired(false);

            builder.HasOne(x => x.User)
                   .WithMany(x => x.UserLoginTokens)
                   .HasForeignKey(x => x.UserId)
                   .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
