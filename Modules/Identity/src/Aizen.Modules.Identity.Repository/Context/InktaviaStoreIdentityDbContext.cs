using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Aizen.Modules.Identity.Domain.Entities;
using Aizen.Modules.Identity.Domain.Entities.UserAgreement;
using Aizen.Modules.Identity.Domain.Entities.UserExternalLogin;
using Aizen.Modules.Identity.Domain.Entities.OtpLogin;
using Aizen.Modules.Identity.Domain.Entities.Onboarding;
using Aizen.Modules.Identity.Domain.Entities.ProviderServiceCategory;
using Aizen.Modules.Identity.Domain.Entities.PasswordRecovery;
using Aizen.Modules.Identity.Domain.Entities.EmailVerification;
using Aizen.Modules.Identity.Domain.Entities.UserValidation;

namespace Aizen.Modules.Identity.Repository.Context
{
    public class IdentityDbContext : IdentityDbContext<
        UserEntity,
        RoleEntity,
        long,
        IdentityUserClaim<long>,
        UserRoleEntity,
        IdentityUserLogin<long>,
        IdentityRoleClaim<long>,
        IdentityUserToken<long>>
    {
        public IdentityDbContext(DbContextOptions<IdentityDbContext> options)
            : base(options)
        {
        }

        public DbSet<UserProfileEntity> UserProfiles { get; set; } = null!;
        public DbSet<UserDeviceEntity> UserDevices { get; set; } = null!;
        public DbSet<UserDeviceBlockEntity> UserDeviceBlocks { get; set; } = null!;
        public DbSet<UserExternalLoginEntity> UserExternalLoginEntities { get; set; } = null!;

        public DbSet<UserMessagePermissionEntity> UserMessagePermissions { get; set; } = null!;
        public DbSet<UserPasswordHistoryEntity> UserPasswordHistories { get; set; } = null!;
        public DbSet<UserLoginTokenEntity> UserLoginTokens { get; set; } = null!;
        public DbSet<UserEmailConfirmEntity> UserEmailConfirms { get; set; } = null!;
        public DbSet<AgreementEntity> Agreements { get; set; } = null!;
        public DbSet<UserAgreementEntity> UserAgreements { get; set; } = null!;
        

        public DbSet<UserValidationEntity> UserValidations { get; set; } = null!;

        public DbSet<VerificationDocumentEntity> VerificationDocuments { get; set; } = null!;
        public DbSet<RiskSignalEntity> RiskSignals { get; set; } = null!;
        public DbSet<ProviderPasswordRecoveryRequestEntity> ProviderPasswordRecoveryRequests { get; set; } = null!;
        public DbSet<ParticipantPasswordRecoveryRequestEntity> ParticipantPasswordRecoveryRequests { get; set; } = null!;
        public DbSet<ProviderOtpLoginRequestEntity> ProviderOtpLoginRequests { get; set; } = null!;
        public DbSet<ProviderEmailVerificationEntity> ProviderEmailVerifications { get; set; } = null!;
        public DbSet<AdminOtpLoginRequestEntity> AdminOtpLoginRequests { get; set; } = null!;
        public DbSet<ParticipantOtpLoginRequestEntity> ParticipantOtpLoginRequests { get; set; } = null!;
        public DbSet<ProviderOnboardingEntity> ProviderOnboarding { get; set; } = null!;
        public DbSet<ProviderServiceCategoryEntity> ProviderServiceCategories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfiguration(new UserEntityConfiguration());
            modelBuilder.ApplyConfiguration(new UserProfileEntityConfiguration());
            modelBuilder.ApplyConfiguration(new UserRoleEntityConfiguration());
            modelBuilder.ApplyConfiguration(new UserDeviceEntityConfiguration());
            modelBuilder.ApplyConfiguration(new UserMessagePermissionEntityConfiguration());
            modelBuilder.ApplyConfiguration(new UserPasswordHistoryEntityConfiguration());
            modelBuilder.ApplyConfiguration(new UserLoginTokenEntityConfiguration());
            modelBuilder.ApplyConfiguration(new UserExternalLoginEntityConfiguration());

            modelBuilder.ApplyConfiguration(new UserEmailConfirmEntityConfiguration());
            modelBuilder.ApplyConfiguration(new UserValidationEntityConfiguration());
            modelBuilder.ApplyConfiguration(new VerificationDocumentEntityConfiguration());
            modelBuilder.ApplyConfiguration(new RiskSignalEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ProviderPasswordRecoveryRequestEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ParticipantPasswordRecoveryRequestEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ProviderOtpLoginRequestEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ProviderEmailVerificationEntityConfiguration());
            modelBuilder.ApplyConfiguration(new AdminOtpLoginRequestEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ParticipantOtpLoginRequestEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ProviderOnboardingEntityConfiguration());
            modelBuilder.ApplyConfiguration(new ProviderServiceCategoryEntityConfiguration());
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            NormalizeDateTimeProperties();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            NormalizeDateTimeProperties();
            return base.SaveChanges();
        }

        private void NormalizeDateTimeProperties()
        {
            foreach (var entry in ChangeTracker.Entries())
            {
                if (entry.State is not (EntityState.Added or EntityState.Modified))
                    continue;

                foreach (var property in entry.Properties)
                {
                    if (property.CurrentValue is DateTime dt && dt.Kind == DateTimeKind.Local)
                    {
                        property.CurrentValue = dt.ToUniversalTime();
                    }
                }
            }
        }
    }
}