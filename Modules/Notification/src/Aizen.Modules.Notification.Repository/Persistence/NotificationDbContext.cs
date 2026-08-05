using Aizen.Core.EFCore;
using Aizen.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Repository.Persistence;

public sealed class NotificationDbContext : AizenDbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<NotificationEntity>           Notifications           => Set<NotificationEntity>();
    public DbSet<NotificationTemplateEntity>   NotificationTemplates   => Set<NotificationTemplateEntity>();
    public DbSet<UserDeviceTokenEntity>        UserDeviceTokens        => Set<UserDeviceTokenEntity>();
    public DbSet<NotificationPreferenceEntity> NotificationPreferences => Set<NotificationPreferenceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("notification");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
    }
}
