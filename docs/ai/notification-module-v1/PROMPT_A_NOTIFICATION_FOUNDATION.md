# PROMPT A — Notification Module: Foundation (Abstraction + Domain + Repository)

## Scope

Implement the foundation layer of `Aizen.Modules.Notification` across three projects:

1. `Aizen.Modules.Notification.Abstraction` — Enums, DTOs, integration message contracts, repository/service interfaces
2. `Aizen.Modules.Notification.Domain` — Domain entities with business methods
3. `Aizen.Modules.Notification.Repository` — EF Core DbContext (`notification` schema), entity configurations, repository implementations

Remove the placeholder `Class1.cs` from each project before starting.

---

## Architecture Rules

- Follow the exact same project/namespace/DI patterns as `Aizen.Modules.ServiceRequest`, `Aizen.Modules.Messaging`, and `Aizen.Modules.Identity`.
- Namespace root: `Aizen.Modules.Notification`
- All entities extend `AizenEntity<long>` (or the project's base entity class).
- All repositories are interface-first, registered via `AddScoped` in a `DependencyInjection.cs` extension.
- DbContext schema: `notification` (PostgreSQL lowercase schema).
- All `DateTimeOffset` fields must be stored UTC-safe.
- Do not mix domain logic into repository or application layers.

---

## STEP 1 — Abstraction Project

### 1.1 Enums

**File:** `Aizen.Modules.Notification.Abstraction/Enum/NotificationChannel.cs`
```csharp
namespace Aizen.Modules.Notification.Abstraction.Enum;

public enum NotificationChannel
{
    InApp = 1,
    Push  = 2,
    Email = 3,
    Sms   = 4,
}
```

**File:** `Aizen.Modules.Notification.Abstraction/Enum/NotificationStatus.cs`
```csharp
namespace Aizen.Modules.Notification.Abstraction.Enum;

public enum NotificationStatus
{
    Pending   = 0,
    Sent      = 1,
    Delivered = 2,
    Failed    = 3,
    Read      = 4,
}
```

**File:** `Aizen.Modules.Notification.Abstraction/Enum/NotificationType.cs`
```csharp
namespace Aizen.Modules.Notification.Abstraction.Enum;

/// <summary>
/// Maps 1-to-1 with business domain events across all modules.
/// Ranges: 100–199 ServiceRequest, 200–299 Messaging, 300–399 CargoDry, 400–499 Identity, 900+ Admin.
/// </summary>
public enum NotificationType
{
    // ── ServiceRequest ───────────────────────────────────────────────────────
    ServiceRequestCreated        = 100,
    ServiceRequestStatusChanged  = 101,
    OfferCreated                 = 110,
    OfferAccepted                = 111,
    OfferRejected                = 112,
    AssignmentCreated            = 120,
    AssignmentAccepted           = 121,
    AssignmentRejected           = 122,
    CompletionSubmitted          = 130,
    CompletionApproved           = 131,
    CompletionRejected           = 132,
    DisputeOpened                = 140,
    DisputeResolved              = 141,
    PaymentReleased              = 150,

    // ── Messaging ────────────────────────────────────────────────────────────
    NewMessageReceived           = 200,
    MessageBlocked               = 201,

    // ── CargoDry ─────────────────────────────────────────────────────────────
    CargoDryKitActivated         = 300,
    CargoDryKitExpiringReminder  = 301,
    CargoDryKitExpired           = 302,

    // ── Identity / Admin ─────────────────────────────────────────────────────
    ProfileApprovalDecision      = 400,

    // ── Admin ─────────────────────────────────────────────────────────────────
    AdminBroadcast               = 900,
}
```

**File:** `Aizen.Modules.Notification.Abstraction/Enum/PushPlatform.cs`
```csharp
namespace Aizen.Modules.Notification.Abstraction.Enum;

public enum PushPlatform
{
    Fcm  = 1,  // Firebase Cloud Messaging (Android + Web)
    Apns = 2,  // Apple Push Notification Service (iOS)
}
```

---

### 1.2 DTOs

**File:** `Aizen.Modules.Notification.Abstraction/Dto/NotificationDto.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Dto;

public sealed class NotificationDto
{
    public long                Id                  { get; init; }
    public NotificationType    Type                { get; init; }
    public NotificationChannel Channel             { get; init; }
    public NotificationStatus  Status              { get; init; }
    public string              Title               { get; init; } = default!;
    public string              Body                { get; init; } = default!;
    public string?             MetadataJson        { get; init; }
    public bool                IsRead              { get; init; }
    public DateTimeOffset      CreatedAt           { get; init; }
    public DateTimeOffset?     ReadAt              { get; init; }
}
```

**File:** `Aizen.Modules.Notification.Abstraction/Dto/NotificationTemplateDto.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Dto;

public sealed class NotificationTemplateDto
{
    public long                Id             { get; init; }
    public string              TemplateCode   { get; init; } = default!;
    public string              Name           { get; init; } = default!;
    public NotificationType    Type           { get; init; }
    public NotificationChannel Channel        { get; init; }
    public string              TitleTemplate  { get; init; } = default!;
    public string              BodyTemplate   { get; init; } = default!;
    public bool                IsActive       { get; init; }
}
```

---

### 1.3 Integration Message Contracts

These are published BY the Notification module after a notification is dispatched, so other modules can react if needed.

**File:** `Aizen.Modules.Notification.Abstraction/Message/NotificationSentMessage.cs`
```csharp
using Aizen.Core.Messagebus.Abstraction.Messages;
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Abstraction.Message;

/// <summary>
/// Published by the Notification module after a notification is dispatched to a channel.
/// Consumers: analytics, delivery audit, CargoDry follow-up.
/// </summary>
public sealed class NotificationSentMessage : AizenBaseMessage
{
    public long                NotificationId    { get; set; }
    public long                RecipientUserId   { get; set; }
    public NotificationType    Type              { get; set; }
    public NotificationChannel Channel           { get; set; }
    public NotificationStatus  Status            { get; set; }
    public string              Title             { get; set; } = default!;
    public DateTimeOffset      SentAt            { get; set; }
}
```

---

### 1.4 Repository Interfaces

**File:** `Aizen.Modules.Notification.Abstraction/Interface/Repository/INotificationRepository.cs`
```csharp
using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Abstraction.Interface.Repository;

public interface INotificationRepository
{
    Task<NotificationEntity?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<List<NotificationEntity>> GetByRecipientAsync(long userId, int skip, int take, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(long userId, CancellationToken ct = default);
    Task AddAsync(NotificationEntity entity, CancellationToken ct = default);
    Task BulkMarkAsReadAsync(long userId, CancellationToken ct = default);
}
```

**File:** `Aizen.Modules.Notification.Abstraction/Interface/Repository/INotificationTemplateRepository.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Abstraction.Interface.Repository;

public interface INotificationTemplateRepository
{
    Task<NotificationTemplateEntity?> GetByCodeAsync(string templateCode, CancellationToken ct = default);
    Task<NotificationTemplateEntity?> GetActiveByTypeAndChannelAsync(
        NotificationType type, NotificationChannel channel, CancellationToken ct = default);
    Task<List<NotificationTemplateEntity>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(NotificationTemplateEntity entity, CancellationToken ct = default);
    Task UpdateAsync(NotificationTemplateEntity entity, CancellationToken ct = default);
}
```

**File:** `Aizen.Modules.Notification.Abstraction/Interface/Repository/IUserDeviceTokenRepository.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Abstraction.Interface.Repository;

public interface IUserDeviceTokenRepository
{
    Task<List<UserDeviceTokenEntity>> GetActiveByUserAsync(long userId, CancellationToken ct = default);
    Task<UserDeviceTokenEntity?> GetByTokenAsync(string token, CancellationToken ct = default);
    Task UpsertAsync(long userId, string token, PushPlatform platform, CancellationToken ct = default);
    Task DeactivateAsync(string token, CancellationToken ct = default);
}
```

### 1.5 Service Interfaces

**File:** `Aizen.Modules.Notification.Abstraction/Interface/Service/INotificationDispatcher.cs`
```csharp
using Aizen.Modules.Notification.Domain.Entities;

namespace Aizen.Modules.Notification.Abstraction.Interface.Service;

/// <summary>
/// Dispatches a persisted notification to its delivery channel(s).
/// Implementations: InAppNotificationDispatcher, PushNotificationDispatcher.
/// CompositeNotificationDispatcher chains all registered dispatchers.
/// </summary>
public interface INotificationDispatcher
{
    Task DispatchAsync(NotificationEntity notification, CancellationToken ct = default);
}
```

**File:** `Aizen.Modules.Notification.Abstraction/Interface/Service/ITemplateInterpolator.cs`
```csharp
namespace Aizen.Modules.Notification.Abstraction.Interface.Service;

/// <summary>
/// Resolves {{variable}} placeholders in notification templates.
/// </summary>
public interface ITemplateInterpolator
{
    string Interpolate(string template, IReadOnlyDictionary<string, string> variables);
}
```

---

## STEP 2 — Domain Project

### 2.1 Notification Entity

**File:** `Aizen.Modules.Notification.Domain/Entities/NotificationEntity.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Domain.Entities;

/// <summary>
/// Persisted notification record. One row per recipient × channel × event.
/// </summary>
public sealed class NotificationEntity : AizenEntity<long>
{
    public long                RecipientUserId    { get; private set; }
    public NotificationType    Type               { get; private set; }
    public NotificationChannel Channel            { get; private set; }
    public string              TemplateCode       { get; private set; } = default!;
    public string              Title              { get; private set; } = default!;
    public string              Body               { get; private set; } = default!;
    public NotificationStatus  Status             { get; private set; }
    public string?             DeliveryProviderRef{ get; private set; }
    public string?             MetadataJson       { get; private set; }
    public DateTimeOffset      CreatedAt          { get; private set; }
    public DateTimeOffset?     SentAt             { get; private set; }
    public DateTimeOffset?     ReadAt             { get; private set; }

    private NotificationEntity() { }

    public static NotificationEntity Create(
        long recipientUserId,
        NotificationType type,
        NotificationChannel channel,
        string templateCode,
        string title,
        string body,
        string? metadataJson = null)
    {
        return new NotificationEntity
        {
            RecipientUserId = recipientUserId,
            Type            = type,
            Channel         = channel,
            TemplateCode    = templateCode,
            Title           = title,
            Body            = body,
            Status          = NotificationStatus.Pending,
            MetadataJson    = metadataJson,
            CreatedAt       = DateTimeOffset.UtcNow,
        };
    }

    public void MarkAsSent(string? providerRef = null)
    {
        Status             = NotificationStatus.Sent;
        SentAt             = DateTimeOffset.UtcNow;
        DeliveryProviderRef = providerRef;
    }

    public void MarkAsFailed()
    {
        Status = NotificationStatus.Failed;
    }

    public void MarkAsRead()
    {
        if (Status == NotificationStatus.Read) return;
        Status = NotificationStatus.Read;
        ReadAt = DateTimeOffset.UtcNow;
    }

    public bool IsRead => ReadAt.HasValue;
}
```

### 2.2 Notification Template Entity

**File:** `Aizen.Modules.Notification.Domain/Entities/NotificationTemplateEntity.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Domain.Entities;

/// <summary>
/// Admin-manageable notification template with {{variable}} interpolation.
/// Template variables use double curly braces: {{requestCode}}, {{providerName}}, etc.
/// </summary>
public sealed class NotificationTemplateEntity : AizenEntity<long>
{
    public string              TemplateCode  { get; private set; } = default!;
    public string              Name          { get; private set; } = default!;
    public NotificationType    Type          { get; private set; }
    public NotificationChannel Channel       { get; private set; }
    public string              TitleTemplate { get; private set; } = default!;
    public string              BodyTemplate  { get; private set; } = default!;
    public bool                IsActive      { get; private set; }
    public DateTimeOffset      CreatedAt     { get; private set; }
    public DateTimeOffset?     UpdatedAt     { get; private set; }

    private NotificationTemplateEntity() { }

    public static NotificationTemplateEntity Create(
        string templateCode,
        string name,
        NotificationType type,
        NotificationChannel channel,
        string titleTemplate,
        string bodyTemplate)
    {
        return new NotificationTemplateEntity
        {
            TemplateCode  = templateCode.ToUpperInvariant(),
            Name          = name,
            Type          = type,
            Channel       = channel,
            TitleTemplate = titleTemplate,
            BodyTemplate  = bodyTemplate,
            IsActive      = true,
            CreatedAt     = DateTimeOffset.UtcNow,
        };
    }

    public void Update(string name, string titleTemplate, string bodyTemplate)
    {
        Name          = name;
        TitleTemplate = titleTemplate;
        BodyTemplate  = bodyTemplate;
        UpdatedAt     = DateTimeOffset.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive  = isActive;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}
```

### 2.3 User Device Token Entity

**File:** `Aizen.Modules.Notification.Domain/Entities/UserDeviceTokenEntity.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Enum;

namespace Aizen.Modules.Notification.Domain.Entities;

/// <summary>
/// FCM/APNS device token registered by a user's mobile or web app.
/// One user can have multiple tokens (multiple devices).
/// </summary>
public sealed class UserDeviceTokenEntity : AizenEntity<long>
{
    public long            UserId       { get; private set; }
    public string          DeviceToken  { get; private set; } = default!;
    public PushPlatform    Platform     { get; private set; }
    public bool            IsActive     { get; private set; }
    public DateTimeOffset  RegisteredAt { get; private set; }
    public DateTimeOffset  LastActiveAt { get; private set; }

    private UserDeviceTokenEntity() { }

    public static UserDeviceTokenEntity Create(long userId, string token, PushPlatform platform)
    {
        return new UserDeviceTokenEntity
        {
            UserId       = userId,
            DeviceToken  = token,
            Platform     = platform,
            IsActive     = true,
            RegisteredAt = DateTimeOffset.UtcNow,
            LastActiveAt = DateTimeOffset.UtcNow,
        };
    }

    public void Refresh()       => LastActiveAt = DateTimeOffset.UtcNow;
    public void Deactivate()    => IsActive = false;
}
```

---

## STEP 3 — Repository Project

### 3.1 DbContext

**File:** `Aizen.Modules.Notification.Repository/Persistence/NotificationDbContext.cs`
```csharp
using Aizen.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Repository.Persistence;

public sealed class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<NotificationEntity>         Notifications      => Set<NotificationEntity>();
    public DbSet<NotificationTemplateEntity> NotificationTemplates => Set<NotificationTemplateEntity>();
    public DbSet<UserDeviceTokenEntity>      UserDeviceTokens   => Set<UserDeviceTokenEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notification");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
```

### 3.2 Entity Configurations

**File:** `Aizen.Modules.Notification.Repository/Persistence/Configurations/NotificationEntityConfiguration.cs`
```csharp
using Aizen.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Notification.Repository.Persistence.Configurations;

public sealed class NotificationEntityConfiguration : IEntityTypeConfiguration<NotificationEntity>
{
    public void Configure(EntityTypeBuilder<NotificationEntity> builder)
    {
        builder.ToTable("notifications");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RecipientUserId).IsRequired();
        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.Channel).IsRequired();
        builder.Property(x => x.TemplateCode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(255).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.DeliveryProviderRef).HasMaxLength(255);
        builder.Property(x => x.MetadataJson).HasColumnType("jsonb");
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.SentAt);
        builder.Property(x => x.ReadAt);

        // Indexes for common queries
        builder.HasIndex(x => new { x.RecipientUserId, x.Status });
        builder.HasIndex(x => new { x.RecipientUserId, x.CreatedAt });
    }
}
```

**File:** `Aizen.Modules.Notification.Repository/Persistence/Configurations/NotificationTemplateEntityConfiguration.cs`
```csharp
using Aizen.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Notification.Repository.Persistence.Configurations;

public sealed class NotificationTemplateEntityConfiguration : IEntityTypeConfiguration<NotificationTemplateEntity>
{
    public void Configure(EntityTypeBuilder<NotificationTemplateEntity> builder)
    {
        builder.ToTable("notification_templates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.TemplateCode).HasMaxLength(100).IsRequired();
        builder.HasIndex(x => x.TemplateCode).IsUnique();

        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.Channel).IsRequired();
        builder.Property(x => x.TitleTemplate).HasMaxLength(500).IsRequired();
        builder.Property(x => x.BodyTemplate).HasMaxLength(4000).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt);

        builder.HasIndex(x => new { x.Type, x.Channel, x.IsActive });
    }
}
```

**File:** `Aizen.Modules.Notification.Repository/Persistence/Configurations/UserDeviceTokenEntityConfiguration.cs`
```csharp
using Aizen.Modules.Notification.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aizen.Modules.Notification.Repository.Persistence.Configurations;

public sealed class UserDeviceTokenEntityConfiguration : IEntityTypeConfiguration<UserDeviceTokenEntity>
{
    public void Configure(EntityTypeBuilder<UserDeviceTokenEntity> builder)
    {
        builder.ToTable("user_device_tokens");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId).IsRequired();
        builder.Property(x => x.DeviceToken).HasMaxLength(512).IsRequired();
        builder.HasIndex(x => x.DeviceToken).IsUnique();

        builder.Property(x => x.Platform).IsRequired();
        builder.Property(x => x.IsActive).IsRequired();
        builder.Property(x => x.RegisteredAt).IsRequired();
        builder.Property(x => x.LastActiveAt).IsRequired();

        builder.HasIndex(x => new { x.UserId, x.IsActive });
    }
}
```

### 3.3 Repositories

**File:** `Aizen.Modules.Notification.Repository/Repositories/NotificationRepository.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Interface.Repository;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Repository.Repositories;

public sealed class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _db;

    public NotificationRepository(NotificationDbContext db) => _db = db;

    public Task<NotificationEntity?> GetByIdAsync(long id, CancellationToken ct)
        => _db.Notifications.FirstOrDefaultAsync(x => x.Id == id, ct);

    public Task<List<NotificationEntity>> GetByRecipientAsync(
        long userId, int skip, int take, CancellationToken ct)
        => _db.Notifications
            .Where(x => x.RecipientUserId == userId)
            .OrderByDescending(x => x.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public Task<int> GetUnreadCountAsync(long userId, CancellationToken ct)
        => _db.Notifications
            .CountAsync(x => x.RecipientUserId == userId && x.ReadAt == null, ct);

    public async Task AddAsync(NotificationEntity entity, CancellationToken ct)
    {
        await _db.Notifications.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task BulkMarkAsReadAsync(long userId, CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;
        await _db.Notifications
            .Where(x => x.RecipientUserId == userId && x.ReadAt == null)
            .ExecuteUpdateAsync(setters =>
                setters
                    .SetProperty(x => x.ReadAt, now)
                    .SetProperty(x => x.Status, Abstraction.Enum.NotificationStatus.Read),
                ct);
    }
}
```

**File:** `Aizen.Modules.Notification.Repository/Repositories/NotificationTemplateRepository.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Interface.Repository;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Repository.Repositories;

public sealed class NotificationTemplateRepository : INotificationTemplateRepository
{
    private readonly NotificationDbContext _db;

    public NotificationTemplateRepository(NotificationDbContext db) => _db = db;

    public Task<NotificationTemplateEntity?> GetByCodeAsync(string templateCode, CancellationToken ct)
        => _db.NotificationTemplates
            .FirstOrDefaultAsync(x => x.TemplateCode == templateCode.ToUpperInvariant(), ct);

    public Task<NotificationTemplateEntity?> GetActiveByTypeAndChannelAsync(
        NotificationType type, NotificationChannel channel, CancellationToken ct)
        => _db.NotificationTemplates
            .FirstOrDefaultAsync(x => x.Type == type && x.Channel == channel && x.IsActive, ct);

    public Task<List<NotificationTemplateEntity>> GetAllAsync(CancellationToken ct)
        => _db.NotificationTemplates.OrderBy(x => x.Type).ThenBy(x => x.Channel).ToListAsync(ct);

    public async Task AddAsync(NotificationTemplateEntity entity, CancellationToken ct)
    {
        await _db.NotificationTemplates.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(NotificationTemplateEntity entity, CancellationToken ct)
    {
        _db.NotificationTemplates.Update(entity);
        await _db.SaveChangesAsync(ct);
    }
}
```

**File:** `Aizen.Modules.Notification.Repository/Repositories/UserDeviceTokenRepository.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Abstraction.Interface.Repository;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Repository.Repositories;

public sealed class UserDeviceTokenRepository : IUserDeviceTokenRepository
{
    private readonly NotificationDbContext _db;

    public UserDeviceTokenRepository(NotificationDbContext db) => _db = db;

    public Task<List<UserDeviceTokenEntity>> GetActiveByUserAsync(long userId, CancellationToken ct)
        => _db.UserDeviceTokens
            .Where(x => x.UserId == userId && x.IsActive)
            .ToListAsync(ct);

    public Task<UserDeviceTokenEntity?> GetByTokenAsync(string token, CancellationToken ct)
        => _db.UserDeviceTokens.FirstOrDefaultAsync(x => x.DeviceToken == token, ct);

    public async Task UpsertAsync(long userId, string token, PushPlatform platform, CancellationToken ct)
    {
        var existing = await GetByTokenAsync(token, ct);
        if (existing is not null)
        {
            existing.Refresh();
            _db.UserDeviceTokens.Update(existing);
        }
        else
        {
            var entity = UserDeviceTokenEntity.Create(userId, token, platform);
            await _db.UserDeviceTokens.AddAsync(entity, ct);
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeactivateAsync(string token, CancellationToken ct)
    {
        var entity = await GetByTokenAsync(token, ct);
        if (entity is null) return;
        entity.Deactivate();
        _db.UserDeviceTokens.Update(entity);
        await _db.SaveChangesAsync(ct);
    }
}
```

### 3.4 Seed Data

**File:** `Aizen.Modules.Notification.Repository/Seed/NotificationTemplateSeed.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Enum;
using Aizen.Modules.Notification.Domain.Entities;
using Aizen.Modules.Notification.Repository.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Aizen.Modules.Notification.Repository.Seed;

/// <summary>
/// Seeds default notification templates. Protected against duplicate inserts by TemplateCode uniqueness.
/// </summary>
public sealed class NotificationTemplateSeed
{
    private readonly NotificationDbContext _db;
    private readonly ILogger<NotificationTemplateSeed> _logger;

    public NotificationTemplateSeed(
        NotificationDbContext db,
        ILogger<NotificationTemplateSeed> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var templates = BuildTemplates();

        foreach (var tpl in templates)
        {
            var exists = await _db.NotificationTemplates
                .AnyAsync(x => x.TemplateCode == tpl.TemplateCode, ct);

            if (!exists)
            {
                await _db.NotificationTemplates.AddAsync(tpl, ct);
                _logger.LogInformation("Seeding notification template: {Code}", tpl.TemplateCode);
            }
        }

        await _db.SaveChangesAsync(ct);
    }

    private static List<NotificationTemplateEntity> BuildTemplates() =>
    [
        // ServiceRequest — InApp
        NotificationTemplateEntity.Create(
            "SR_CREATED_INAPP", "Service Request Created (In-App)",
            NotificationType.ServiceRequestCreated, NotificationChannel.InApp,
            "New Service Request: {{requestCode}}",
            "A new service request {{requestCode}} has been created for {{serviceName}}."),

        NotificationTemplateEntity.Create(
            "SR_STATUS_CHANGED_INAPP", "Service Request Status Changed (In-App)",
            NotificationType.ServiceRequestStatusChanged, NotificationChannel.InApp,
            "Request {{requestCode}} Status Updated",
            "Your request {{requestCode}} moved from {{fromStatus}} to {{toStatus}}."),

        NotificationTemplateEntity.Create(
            "SR_OFFER_CREATED_INAPP", "Offer Received (In-App)",
            NotificationType.OfferCreated, NotificationChannel.InApp,
            "New Offer on {{requestCode}}",
            "{{providerName}} submitted a new offer for your request {{requestCode}}."),

        NotificationTemplateEntity.Create(
            "SR_OFFER_ACCEPTED_INAPP", "Offer Accepted (In-App)",
            NotificationType.OfferAccepted, NotificationChannel.InApp,
            "Your Offer Was Accepted",
            "Your offer on request {{requestCode}} has been accepted by {{ownerName}}."),

        NotificationTemplateEntity.Create(
            "SR_ASSIGNMENT_CREATED_INAPP", "Assignment Created (In-App)",
            NotificationType.AssignmentCreated, NotificationChannel.InApp,
            "You've Been Assigned to {{requestCode}}",
            "You have been assigned to service request {{requestCode}}. Please review and confirm."),

        NotificationTemplateEntity.Create(
            "SR_COMPLETION_SUBMITTED_INAPP", "Completion Submitted (In-App)",
            NotificationType.CompletionSubmitted, NotificationChannel.InApp,
            "Completion Submitted: {{requestCode}}",
            "{{providerName}} has submitted completion for request {{requestCode}}. Please review."),

        NotificationTemplateEntity.Create(
            "SR_DISPUTE_OPENED_INAPP", "Dispute Opened (In-App)",
            NotificationType.DisputeOpened, NotificationChannel.InApp,
            "Dispute Opened: {{requestCode}}",
            "A dispute has been opened for service request {{requestCode}}."),

        NotificationTemplateEntity.Create(
            "SR_PAYMENT_RELEASED_INAPP", "Payment Released (In-App)",
            NotificationType.PaymentReleased, NotificationChannel.InApp,
            "Payment Released: {{requestCode}}",
            "Payment for service request {{requestCode}} has been released."),

        // Messaging — InApp
        NotificationTemplateEntity.Create(
            "MSG_NEW_MESSAGE_INAPP", "New Message (In-App)",
            NotificationType.NewMessageReceived, NotificationChannel.InApp,
            "New message from {{senderName}}",
            "{{senderName}} sent a message in conversation {{conversationTitle}}."),

        // CargoDry — InApp
        NotificationTemplateEntity.Create(
            "CD_KIT_EXPIRING_INAPP", "Kit Expiring Reminder (In-App)",
            NotificationType.CargoDryKitExpiringReminder, NotificationChannel.InApp,
            "CargoDry Kit Expiring Soon",
            "Your CargoDry kit {{kitCode}} expires in {{daysLeft}} days. Please renew."),

        NotificationTemplateEntity.Create(
            "CD_KIT_ACTIVATED_INAPP", "Kit Activated (In-App)",
            NotificationType.CargoDryKitActivated, NotificationChannel.InApp,
            "CargoDry Kit Activated",
            "Your CargoDry kit {{kitCode}} has been successfully activated."),

        // Identity — InApp
        NotificationTemplateEntity.Create(
            "PROFILE_APPROVAL_DECISION_INAPP", "Profile Approval Decision (In-App)",
            NotificationType.ProfileApprovalDecision, NotificationChannel.InApp,
            "Profile Review Complete",
            "Your profile has been {{decision}}. {{reason}}"),
    ];
}
```

### 3.5 DI Registration

**File:** `Aizen.Modules.Notification.Repository/DependencyInjection.cs`
```csharp
using Aizen.Modules.Notification.Abstraction.Interface.Repository;
using Aizen.Modules.Notification.Repository.Persistence;
using Aizen.Modules.Notification.Repository.Repositories;
using Aizen.Modules.Notification.Repository.Seed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;

namespace Aizen.Modules.Notification.Repository;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationRepository(this IServiceCollection services)
    {
        services.AddScoped<INotificationRepository,         NotificationRepository>();
        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<IUserDeviceTokenRepository,      UserDeviceTokenRepository>();
        services.AddScoped<NotificationTemplateSeed>();
        return services;
    }

    public static async Task SeedNotificationAsync(this IHost host, CancellationToken ct = default)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();

        var pending = await db.Database.GetPendingMigrationsAsync(ct);
        if (pending.Any())
            await db.Database.MigrateAsync(ct);

        var seeder = scope.ServiceProvider.GetRequiredService<NotificationTemplateSeed>();
        await seeder.SeedAsync(ct);
    }
}
```

---

## STEP 4 — csproj Package References

### `Aizen.Modules.Notification.Abstraction.csproj`
```xml
<PackageReference Include="Aizen.Core.Messagebus.Abstraction" Version="*" />
```

### `Aizen.Modules.Notification.Domain.csproj`
```xml
<ProjectReference Include="..\Aizen.Modules.Notification.Abstraction\Aizen.Modules.Notification.Abstraction.csproj" />
```

### `Aizen.Modules.Notification.Repository.csproj`
```xml
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.*" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="9.*" />
<ProjectReference Include="..\Aizen.Modules.Notification.Domain\Aizen.Modules.Notification.Domain.csproj" />
<ProjectReference Include="..\Aizen.Modules.Notification.Abstraction\Aizen.Modules.Notification.Abstraction.csproj" />
```

---

## Migration Command

After the repository is wired into Program.cs (PROMPT C), run:
```bash
dotnet ef migrations add InitialCreate \
  --project Aizen.Modules.Notification.Repository \
  --startup-project Aizen.Modules.Notification \
  --context NotificationDbContext \
  --output-dir Persistence/Migrations
```

---

## Verification Checklist

- [ ] All 3 projects build with `dotnet build` — no errors
- [ ] `NotificationEntity.Create()` → `MarkAsSent()` → `MarkAsRead()` chain compiles
- [ ] `NotificationDbContext` schema = `"notification"`, all tables in lowercase
- [ ] `NotificationTemplateSeed` does not insert duplicate records (unique index on `TemplateCode`)
- [ ] `INotificationRepository`, `INotificationTemplateRepository`, `IUserDeviceTokenRepository` all registered in DI
