using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Identity.Domain.Entities.Onboarding;

public class ProviderOnboardingEntityConfiguration
    : IEntityTypeConfiguration<ProviderOnboardingEntity>
{
    public void Configure(EntityTypeBuilder<ProviderOnboardingEntity> builder)
    {
        builder.ToTable("provider_onboarding");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProfileId).IsRequired();
        builder.HasIndex(x => x.ProfileId).IsUnique();

        builder.Property(x => x.UserId).IsRequired();
        builder.HasIndex(x => x.UserId);

        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.SchemaVersion).IsRequired().HasDefaultValue(1);

        builder.Property(x => x.StepStatusesJson)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasDefaultValue("{}");

        builder.Property(x => x.DraftJson)
               .IsRequired()
               .HasColumnType("jsonb")
               .HasDefaultValue("{}");

        builder.Property(x => x.RevisionStepsJson)
               .HasColumnType("jsonb");
    }
}
