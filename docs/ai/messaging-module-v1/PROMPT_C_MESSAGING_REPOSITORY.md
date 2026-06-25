# PROMPT C — Messaging Module: Repository Layer
# `Aizen.Modules.Messaging.Repository`

## Context & Architecture Rules

You are implementing the **Repository layer** of `Aizen.Modules.Messaging`. This layer contains the EF Core DbContext, entity configurations, repository implementations, DI registration, and seed data.

**Confirmed patterns from ServiceRequest module — follow exactly:**
- DbContext base: `AizenDbContext` (from `Aizen.Core.EFCore`)
- Schema: `modelBuilder.HasDefaultSchema("messaging")`
- Table names: lowercase with underscores — e.g., `conversations`, `conversation_messages`
- `SaveChangesAsync` override normalizes DateTime UTC (copy the same pattern from `ServiceRequestDbContext`)
- DI: `IServiceCollection` extension method `AddMessagingRepository()`
- Seed extension: `IHost.SeedMessagingAsync()` extension method
- `DesignTime` factory class for `dotnet ef` CLI
- Repositories use direct `DbContext` injection — no generic base
- `AsNoTracking()` on all queries, `Include().ThenInclude()` for aggregates
- Namespace: `Aizen.Modules.Messaging.Repository.*`

---

## Step 1 — DbContext (`Persistence/MessagingDbContext.cs`)

```csharp
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

    public DbSet<ConversationEntity>            Conversations        => Set<ConversationEntity>();
    public DbSet<ConversationParticipantEntity> ConversationParticipants => Set<ConversationParticipantEntity>();
    public DbSet<ConversationMessageEntity>     ConversationMessages => Set<ConversationMessageEntity>();
    public DbSet<MessageAttachmentEntity>       MessageAttachments   => Set<MessageAttachmentEntity>();

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
```

---

## Step 2 — EF Core Configurations (`Persistence/Configurations/`)

### `ConversationEntityConfiguration.cs`
```csharp
using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ConversationEntityConfiguration : IEntityTypeConfiguration<ConversationEntity>
{
    public void Configure(EntityTypeBuilder<ConversationEntity> builder)
    {
        builder.ToTable("conversations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Status).HasConversion<int>().IsRequired();
        builder.Property(x => x.ContextType).HasConversion<int>().IsRequired();
        builder.Property(x => x.LastMessagePreview).HasMaxLength(200);

        // Indexes for common query patterns
        builder.HasIndex(x => x.ContextType);
        builder.HasIndex(x => new { x.ContextType, x.ContextId }).IsUnique();   // One conversation per context
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.LastMessageAt);

        // Navigation
        builder.HasMany(x => x.Participants)
            .WithOne()
            .HasForeignKey("ConversationId")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Messages)
            .WithOne()
            .HasForeignKey("ConversationId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### `ConversationParticipantEntityConfiguration.cs`
```csharp
public sealed class ConversationParticipantEntityConfiguration
    : IEntityTypeConfiguration<ConversationParticipantEntity>
{
    public void Configure(EntityTypeBuilder<ConversationParticipantEntity> builder)
    {
        builder.ToTable("conversation_participants");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DisplayName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Role).HasConversion<int>().IsRequired();
        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => new { x.ConversationId, x.UserId });
    }
}
```

### `ConversationMessageEntityConfiguration.cs`
```csharp
public sealed class ConversationMessageEntityConfiguration
    : IEntityTypeConfiguration<ConversationMessageEntity>
{
    public void Configure(EntityTypeBuilder<ConversationMessageEntity> builder)
    {
        builder.ToTable("conversation_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SenderName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.SenderRole).HasConversion<int>().IsRequired();
        builder.Property(x => x.Content).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.Type).HasConversion<int>().IsRequired();
        builder.Property(x => x.ModerationStatus).HasConversion<int>().IsRequired();
        builder.Property(x => x.ModerationReason).HasMaxLength(500);

        builder.HasIndex(x => x.ConversationId);
        builder.HasIndex(x => x.SentAt);
        builder.HasIndex(x => x.ModerationStatus);   // For moderation queue queries

        builder.HasMany(x => x.Attachments)
            .WithOne()
            .HasForeignKey("MessageId")
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### `MessageAttachmentEntityConfiguration.cs`
```csharp
public sealed class MessageAttachmentEntityConfiguration
    : IEntityTypeConfiguration<MessageAttachmentEntity>
{
    public void Configure(EntityTypeBuilder<MessageAttachmentEntity> builder)
    {
        builder.ToTable("message_attachments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FileName).IsRequired().HasMaxLength(300);
        builder.Property(x => x.FileType).HasMaxLength(50);
        builder.Property(x => x.FileStorageId).HasMaxLength(200);
        builder.HasIndex(x => x.MessageId);
    }
}
```

---

## Step 3 — Repository Implementations (`Repositories/`)

### `ConversationRepository.cs`
```csharp
using Aizen.Modules.Messaging.Abstraction.Enum;
using Aizen.Modules.Messaging.Domain.Entities.Conversation;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Aizen.Modules.Messaging.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Messaging.Repository.Repositories;

[DocumentationInfo("Conversation repository", "EF Core implementation of IConversationRepository.")]
public sealed class ConversationRepository : IConversationRepository
{
    private readonly MessagingDbContext _db;
    public ConversationRepository(MessagingDbContext db) => _db = db;

    public async Task<IReadOnlyList<ConversationEntity>> GetListAsync(
        ConversationStatus? status,
        MessagingContextType? contextType,
        int skip, int take,
        CancellationToken ct = default)
    {
        var query = _db.Conversations
            .AsNoTracking()
            .Include(x => x.Participants)
            .Where(x => !x.IsDeleted);

        if (status.HasValue)
            query = query.Where(x => x.Status == status.Value);

        if (contextType.HasValue)
            query = query.Where(x => x.ContextType == contextType.Value);

        return await query
            .OrderByDescending(x => x.LastMessageAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public Task<ConversationEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.Conversations
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<ConversationEntity?> GetByIdWithMessagesAsync(long id, CancellationToken ct = default)
        => _db.Conversations
            .Include(x => x.Participants)
            .Include(x => x.Messages)
                .ThenInclude(m => m.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public Task<ConversationEntity?> GetByContextAsync(
        MessagingContextType contextType, long contextId, CancellationToken ct = default)
        => _db.Conversations
            .Include(x => x.Participants)
            .FirstOrDefaultAsync(x =>
                x.ContextType == contextType &&
                x.ContextId   == contextId   &&
                !x.IsDeleted, ct);

    public Task<int> CountAsync(
        ConversationStatus? status, MessagingContextType? contextType, CancellationToken ct = default)
    {
        var query = _db.Conversations.AsNoTracking().Where(x => !x.IsDeleted);
        if (status.HasValue) query = query.Where(x => x.Status == status.Value);
        if (contextType.HasValue) query = query.Where(x => x.ContextType == contextType.Value);
        return query.CountAsync(ct);
    }

    public Task AddAsync(ConversationEntity entity, CancellationToken ct = default)
        => _db.Conversations.AddAsync(entity, ct).AsTask();

    public void Update(ConversationEntity entity)
        => _db.Conversations.Update(entity);
}
```

### `ConversationMessageRepository.cs`
```csharp
public sealed class ConversationMessageRepository : IConversationMessageRepository
{
    private readonly MessagingDbContext _db;
    public ConversationMessageRepository(MessagingDbContext db) => _db = db;

    public async Task<IReadOnlyList<ConversationMessageEntity>> GetByConversationIdAsync(
        long conversationId, int skip, int take, CancellationToken ct = default)
        => await _db.ConversationMessages
            .AsNoTracking()
            .Include(x => x.Attachments)
            .Where(x => x.ConversationId == conversationId && !x.IsDeleted)
            .OrderBy(x => x.SentAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<ConversationMessageEntity?> GetByIdAsync(long id, CancellationToken ct = default)
        => _db.ConversationMessages
            .Include(x => x.Attachments)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);

    public async Task<IReadOnlyList<ConversationMessageEntity>> GetFlaggedAsync(
        int skip, int take, CancellationToken ct = default)
        => await _db.ConversationMessages
            .AsNoTracking()
            .Include(x => x.Attachments)
            .Where(x => (x.ModerationStatus == MessageModerationStatus.Flagged
                       || x.ModerationStatus == MessageModerationStatus.PendingReview)
                      && !x.IsDeleted)
            .OrderByDescending(x => x.SentAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task AddAsync(ConversationMessageEntity entity, CancellationToken ct = default)
        => _db.ConversationMessages.AddAsync(entity, ct).AsTask();

    public void Update(ConversationMessageEntity entity)
        => _db.ConversationMessages.Update(entity);
}
```

---

## Step 4 — Design-Time Factory (`DesignTime/MessagingDesignTimeFactory.cs`)

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aizen.Modules.Messaging.Repository.DesignTime;

public sealed class MessagingDesignTimeFactory : IDesignTimeDbContextFactory<MessagingDbContext>
{
    public MessagingDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MessagingDbContext>()
            .UseNpgsql("Host=localhost;Database=aizen_messaging;Username=postgres;Password=postgres")
            .Options;
        return new MessagingDbContext(options);
    }
}
```

---

## Step 5 — DI Registration (`DependencyInjection.cs`)

```csharp
using Aizen.Modules.Messaging.Application.Services;
using Aizen.Modules.Messaging.Domain.Interface;
using Aizen.Modules.Messaging.Domain.Interface.Repository;
using Aizen.Modules.Messaging.Repository.Persistence;
using Aizen.Modules.Messaging.Repository.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Aizen.Modules.Messaging.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddMessagingRepository(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IConversationMessageRepository, ConversationMessageRepository>();

        // Domain services
        services.AddScoped<IMessageContentPolicy, MessageContentPolicyService>();

        return services;
    }

    public static IServiceCollection AddMessagingServices(this IServiceCollection services)
    {
        // Future: add additional application services here
        return services;
    }

    public static async Task SeedMessagingAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MessagingDbContext>();

        var pending = await db.Database.GetPendingMigrationsAsync(ct);
        if (pending.Any())
            await db.Database.MigrateAsync(ct);

        // Seed data hook (implement if MockData seeder is added)
        // var seeder = scope.ServiceProvider.GetRequiredService<MessagingMockDataSeeder>();
        // await seeder.SeedAsync(ct);
    }
}
```

---

## Step 6 — EF Core Migration

Run after all configurations are in place:

```bash
dotnet ef migrations add InitialMessaging \
  --project Aizen.Modules.Messaging.Repository \
  --startup-project Aizen.Modules.Messaging
```

**Expected tables created:**
- `messaging.conversations`
- `messaging.conversation_participants`
- `messaging.conversation_messages`
- `messaging.message_attachments`

**Verify indexes:**
```sql
SELECT indexname, indexdef
FROM pg_indexes
WHERE schemaname = 'messaging'
ORDER BY tablename, indexname;
```

Expected critical indexes:
- `conversations`: `(context_type, context_id)` UNIQUE — enforces one conversation per context
- `conversations`: `(last_message_at DESC)` — for sorted list query
- `conversation_messages`: `(conversation_id)`, `(moderation_status)` — for chat + moderation queue

---

## Step 7 — MongoDB Integration (Optional — for message archival)

The `Aizen.Modules.Messaging.Domain` project already has a reference to `Aizen.Core.Data.Mongo`. Use MongoDB for **archiving old conversations** (>90 days) to reduce PostgreSQL pressure.

Add a MongoDB document class if archival is needed:

```csharp
// MessagingModule/Domain/MongoDocuments/ArchivedConversationDocument.cs
public sealed class ArchivedConversationDocument
{
    [BsonId]
    public string Id { get; set; } = default!;      // "conv:{id}"
    public long ConversationId { get; set; }
    public string ContextType  { get; set; } = default!;
    public long ContextId      { get; set; }
    public string Title        { get; set; } = default!;
    public DateTimeOffset ArchivedAt { get; set; }
    public List<ArchivedMessageDoc> Messages { get; set; } = [];
}

public sealed class ArchivedMessageDoc
{
    public long MessageId    { get; set; }
    public string SenderName { get; set; } = default!;
    public string Content    { get; set; } = default!;
    public DateTimeOffset SentAt { get; set; }
}
```

**This is MVP-optional.** Implement only if there is a clear requirement for archival at this stage.

---

## Quality Gates

- [ ] Migration runs successfully: 4 tables created in `messaging` schema
- [ ] `(context_type, context_id)` UNIQUE index exists — verified in DB
- [ ] `ConversationRepository.GetByContextAsync` returns correct conversation for given SR/order/kit ID
- [ ] `ConversationMessageRepository.GetFlaggedAsync` returns only `Flagged` + `PendingReview` messages
- [ ] All repositories registered in DI
- [ ] `IMessageContentPolicy` → `MessageContentPolicyService` registered as Scoped
- [ ] `SeedMessagingAsync` runs without error on startup (even if no seed data present)
- [ ] DesignTime factory points to correct connection string
