using Aizen.Core.EFCore;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Messaging.Repository.Persistence;

[DocumentationInfo("Messaging EF DbContext",
    "EF Core context for the messaging module PostgreSQL schema.")]
public sealed class MessagingDbContext : AizenDbContext
{
    public MessagingDbContext(DbContextOptions<MessagingDbContext> options)
        : base(options) { }

    public DbSet<ConversationEntity>            Conversations            => Set<ConversationEntity>();
    public DbSet<ConversationParticipantEntity> ConversationParticipants => Set<ConversationParticipantEntity>();
    public DbSet<ConversationMessageEntity>     ConversationMessages     => Set<ConversationMessageEntity>();
    public DbSet<MessageAttachmentEntity>       MessageAttachments       => Set<MessageAttachmentEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("messaging");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MessagingDbContext).Assembly);
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
            if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;
            foreach (var property in entry.Properties)
            {
                if (property.CurrentValue is DateTime dt && dt.Kind == DateTimeKind.Local)
                    property.CurrentValue = dt.ToUniversalTime();
            }
        }
    }
}
