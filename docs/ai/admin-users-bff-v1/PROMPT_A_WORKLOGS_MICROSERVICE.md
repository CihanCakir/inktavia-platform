# WorkLogs — Microservice Implementation Prompt

**Target project:** `Aizen.Modules.ServiceRequest` (the ServiceRequest microservice)
**Scope:** Work Logs feature — entities, EF Core config, CQRS (3 queries + 2 commands), FluentValidation, controller endpoints, EF migration.
**Run this in:** The `Aizen.Modules.ServiceRequest` solution folder in Claude Code.

---

## ⚠️ STEP 0 — MANDATORY reads before writing anything

### 0a. Project structure

```
ls -R Modules/ServiceRequest/src/
```

Identify and note:
- Domain layer folder (likely `Aizen.Modules.ServiceRequest.Domain`)
- Application layer folder (likely `Aizen.Modules.ServiceRequest.Application`)
- Infrastructure/Repository layer folder (likely `Aizen.Modules.ServiceRequest.Repository` or `.Infrastructure`)
- API layer folder (likely `Aizen.Modules.ServiceRequest.Api` or `.Controllers`)

### 0b. Existing `ServiceRequestEntity`

Read the file. Note every field that already exists. You will NOT add duplicates. In particular check:
- Does `WorkLogEntries` navigation property already exist?
- Does `WorkPhases` navigation property already exist?
- What is the `BaseEntity` structure (fields: `Id`, `CreatedAt`, `UpdatedAt`, `IsDeleted`?)

### 0c. Read 2 existing CQRS examples

Find any existing `*Query.cs` + `*QueryHandler.cs` pair and any `*Command.cs` + `*CommandHandler.cs` pair. Note:
- The exact `IRequest<T>` pattern used (`MediatR.IRequest<T>` or custom?)
- Return type wrapper (`Result<T>`, `AizenResponse<T>`, plain `T`?)
- How `CancellationToken ct` is declared and passed
- Handler class declaration (`IRequestHandler<TQuery, TResponse>` + `Handle` method signature)
- Namespace convention (e.g., `Aizen.Modules.ServiceRequest.Application.WorkLogs.Queries`)

### 0d. Read 1 existing controller

Note:
- Base class (e.g., `AizenControllerBase`, `ApiController`, `ControllerBase`)
- Route prefix style (`[Route("api/service-requests")]` or route on each method?)
- How the mediator is called (`_mediator.Send(query, ct)`)
- How response is returned (`Ok(result)`, `AizenOk(result)`, `ApiResponse.Success(result)`)
- Auth attribute style (`[Authorize]` or `[AizenAuthorize]` or similar)

### 0e. Read 1 existing EF configuration

Note:
- Class pattern (`IEntityTypeConfiguration<T>` implementing class)
- Table naming (snake_case? e.g., `"work_log_entries"`)
- Column naming convention
- How `Id` is configured (`.HasKey()`, identity, sequence)
- Soft delete configuration (if `IsDeleted` exists, how it's filtered)

### 0f. Read existing `DbContext`

Note:
- Class name
- Existing `DbSet<>` properties
- Whether it has any base `OnModelCreating` patterns

### 0g. Read 1 existing FluentValidation validator

Note:
- How validators are registered (assembly scan? explicit `services.AddTransient<>?`)
- Whether `AbstractValidator<T>` is the base
- Whether there's a custom `ValidatorBehavior` in the pipeline

### 0h. Check latest migration file

```
ls Modules/ServiceRequest/src/<Infrastructure>/Migrations/ | sort | tail -5
```

Note the naming sequence. Your new migration will follow it (e.g., `20260624_AddWorkLogs`).

**Only after Step 0 is complete, proceed.**

---

## Step 1 — Domain Entities

Add only what is MISSING based on your Step 0b read. Do not duplicate fields.

### 1a. Extend `ServiceRequestEntity` — add missing navigation properties

```csharp
// Add only if MISSING:
public ICollection<WorkLogEntryEntity> WorkLogEntries { get; private set; } = [];
public ICollection<WorkPhaseEntity> WorkPhases { get; private set; } = [];
```

Domain method — add if missing:
```csharp
public void AddWorkLogEntry(
    string type,
    string content,
    string author,
    long? authorId = null,
    string? mediaFileId = null)
{
    var entry = WorkLogEntryEntity.Create(
        serviceRequestId: Id,
        type: type,
        content: content,
        author: author,
        authorId: authorId,
        mediaFileId: mediaFileId);
    WorkLogEntries.Add(entry);
}
```

### 1b. `WorkLogEntryEntity`

Create in `Domain/Entities/WorkLogEntryEntity.cs` (use the existing entities folder found in Step 0b).

```csharp
using System;

namespace Aizen.Modules.ServiceRequest.Domain.Entities;   // adjust namespace to match project

public sealed class WorkLogEntryEntity : BaseEntity   // use the BaseEntity found in Step 0b
{
    // ── Fields ───────────────────────────────────────────────────────────────
    public long ServiceRequestId { get; private set; }

    /// <summary>note | photo | status_change</summary>
    public string Type { get; private set; } = "note";

    public string Content { get; private set; } = string.Empty;

    /// <summary>FileStorage file ID. BFF resolves this to a signed URL — not stored as URL.</summary>
    public string? MediaFileId { get; private set; }

    public string Author { get; private set; } = string.Empty;
    public long? AuthorId { get; private set; }
    public DateTimeOffset Timestamp { get; private set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public ServiceRequestEntity ServiceRequest { get; private set; } = default!;

    // ── EF Core constructor ───────────────────────────────────────────────────
    private WorkLogEntryEntity() { }

    // ── Factory ───────────────────────────────────────────────────────────────
    public static WorkLogEntryEntity Create(
        long serviceRequestId,
        string type,
        string content,
        string author,
        long? authorId = null,
        string? mediaFileId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(author);

        if (!ValidTypes.Contains(type))
            throw new ArgumentException($"Invalid type '{type}'. Allowed: {string.Join(", ", ValidTypes)}");

        return new WorkLogEntryEntity
        {
            ServiceRequestId = serviceRequestId,
            Type = type,
            Content = content,
            Author = author,
            AuthorId = authorId,
            MediaFileId = mediaFileId,
            Timestamp = DateTimeOffset.UtcNow,
        };
    }

    private static readonly HashSet<string> ValidTypes =
        new(StringComparer.OrdinalIgnoreCase) { "note", "photo", "status_change" };
}
```

### 1c. `WorkPhaseEntity`

Create in `Domain/Entities/WorkPhaseEntity.cs`:

```csharp
namespace Aizen.Modules.ServiceRequest.Domain.Entities;

public sealed class WorkPhaseEntity : BaseEntity
{
    public long ServiceRequestId { get; private set; }
    public int PhaseNumber { get; private set; }
    public string Title { get; private set; } = string.Empty;

    /// <summary>0–100</summary>
    public int ProgressPercent { get; private set; }

    /// <summary>Upcoming | In Progress | Completed</summary>
    public string Status { get; private set; } = "Upcoming";

    public int DisplayOrder { get; private set; }

    // ── Navigation ───────────────────────────────────────────────────────────
    public ServiceRequestEntity ServiceRequest { get; private set; } = default!;

    // ── EF constructor ────────────────────────────────────────────────────────
    private WorkPhaseEntity() { }

    // ── Factory ───────────────────────────────────────────────────────────────
    public static WorkPhaseEntity Create(
        long serviceRequestId,
        int phaseNumber,
        string title,
        int displayOrder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);

        return new WorkPhaseEntity
        {
            ServiceRequestId = serviceRequestId,
            PhaseNumber = phaseNumber,
            Title = title,
            ProgressPercent = 0,
            Status = "Upcoming",
            DisplayOrder = displayOrder,
        };
    }

    // ── Domain methods ────────────────────────────────────────────────────────
    public void UpdateProgress(int progressPercent, string status)
    {
        if (!ValidStatuses.Contains(status))
            throw new ArgumentException($"Invalid status '{status}'.");
        ProgressPercent = Math.Clamp(progressPercent, 0, 100);
        Status = status;
    }

    public void MarkInProgress() => UpdateProgress(ProgressPercent, "In Progress");
    public void MarkCompleted()  => UpdateProgress(100, "Completed");

    private static readonly HashSet<string> ValidStatuses =
        new(StringComparer.OrdinalIgnoreCase) { "Upcoming", "In Progress", "Completed" };
}
```

---

## Step 2 — EF Core Configurations

Follow the exact IEntityTypeConfiguration style found in Step 0e.

### 2a. `WorkLogEntryConfiguration`

Create in `Infrastructure/Configurations/WorkLogEntryConfiguration.cs`:

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Aizen.Modules.ServiceRequest.Domain.Entities;

namespace Aizen.Modules.ServiceRequest.Infrastructure.Configurations;  // adjust namespace

public sealed class WorkLogEntryConfiguration : IEntityTypeConfiguration<WorkLogEntryEntity>
{
    public void Configure(EntityTypeBuilder<WorkLogEntryEntity> builder)
    {
        builder.ToTable("work_log_entries");   // snake_case; adjust if convention differs

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceRequestId).IsRequired();
        builder.Property(x => x.Type).IsRequired().HasMaxLength(32);
        builder.Property(x => x.Content).IsRequired().HasMaxLength(4000);
        builder.Property(x => x.MediaFileId).HasMaxLength(256);
        builder.Property(x => x.Author).IsRequired().HasMaxLength(256);
        builder.Property(x => x.AuthorId);
        builder.Property(x => x.Timestamp).IsRequired();

        // ── Indexes ───────────────────────────────────────────────────────────
        builder.HasIndex(x => x.ServiceRequestId)
               .HasDatabaseName("ix_work_log_entries_service_request_id");

        builder.HasIndex(x => x.Timestamp)
               .HasDatabaseName("ix_work_log_entries_timestamp");

        // ── Relationship ──────────────────────────────────────────────────────
        builder.HasOne(x => x.ServiceRequest)
               .WithMany(sr => sr.WorkLogEntries)
               .HasForeignKey(x => x.ServiceRequestId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### 2b. `WorkPhaseConfiguration`

Create in `Infrastructure/Configurations/WorkPhaseConfiguration.cs`:

```csharp
public sealed class WorkPhaseConfiguration : IEntityTypeConfiguration<WorkPhaseEntity>
{
    public void Configure(EntityTypeBuilder<WorkPhaseEntity> builder)
    {
        builder.ToTable("work_phases");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ServiceRequestId).IsRequired();
        builder.Property(x => x.PhaseNumber).IsRequired();
        builder.Property(x => x.Title).IsRequired().HasMaxLength(128);
        builder.Property(x => x.ProgressPercent).IsRequired().HasDefaultValue(0);
        builder.Property(x => x.Status).IsRequired().HasMaxLength(32).HasDefaultValue("Upcoming");
        builder.Property(x => x.DisplayOrder).IsRequired();

        // ── Composite unique: one phase number per service request ────────────
        builder.HasIndex(x => new { x.ServiceRequestId, x.PhaseNumber })
               .IsUnique()
               .HasDatabaseName("uix_work_phases_sr_id_phase_number");

        builder.HasOne(x => x.ServiceRequest)
               .WithMany(sr => sr.WorkPhases)
               .HasForeignKey(x => x.ServiceRequestId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
```

### 2c. Register DbSets in DbContext

In the existing `DbContext` class (found in Step 0f), add:

```csharp
public DbSet<WorkLogEntryEntity> WorkLogEntries => Set<WorkLogEntryEntity>();
public DbSet<WorkPhaseEntity> WorkPhases => Set<WorkPhaseEntity>();
```

---

## Step 3 — Application Layer: Queries

Use the exact CQRS pattern found in Step 0c.

### 3a. `GetWorkLogsQuery`

Create in `Application/WorkLogs/Queries/GetWorkLogs/GetWorkLogsQuery.cs`:

```csharp
using MediatR;  // or the IRequest source found in Step 0c

namespace Aizen.Modules.ServiceRequest.Application.WorkLogs.Queries.GetWorkLogs;

/// <summary>Returns the full work-logs payload for a service request.</summary>
public sealed record GetWorkLogsQuery(long ServiceRequestId) : IRequest<GetWorkLogsResult>;

public sealed record GetWorkLogsResult
{
    public long ServiceRequestId { get; init; }
    public string Title { get; init; } = string.Empty;
    public WorkPhaseResult? CurrentPhase { get; init; }
    public IReadOnlyList<WorkPhaseResult> Phases { get; init; } = [];
    public IReadOnlyList<WorkLogEntryResult> LogEntries { get; init; } = [];
    public ProviderActivityResult ProviderActivity { get; init; } = new();
    public JobHealthResult JobHealth { get; init; } = new();
}

public sealed record WorkPhaseResult
{
    public int Number { get; init; }
    public string Title { get; init; } = string.Empty;
    public int ProgressPercent { get; init; }
    /// <summary>Upcoming | In Progress | Completed</summary>
    public string Status { get; init; } = "Upcoming";
}

public sealed record WorkLogEntryResult
{
    public long Id { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    /// <summary>note | photo | status_change</summary>
    public string Type { get; init; } = "note";
    public string Content { get; init; } = string.Empty;
    /// <summary>Raw file ID — BFF resolves to signed URL.</summary>
    public string? MediaFileId { get; init; }
    public string Author { get; init; } = string.Empty;
}

public sealed record ProviderActivityResult
{
    public string ProviderId { get; init; } = string.Empty;
    public string ProviderName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public IReadOnlyList<ProviderActivityEntryResult> Activities { get; init; } = [];
}

public sealed record ProviderActivityEntryResult
{
    public string Id { get; init; } = string.Empty;
    public DateTimeOffset Timestamp { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Icon { get; init; } = "check_circle";
}

public sealed record JobHealthResult
{
    /// <summary>0–100 budget utilisation score.</summary>
    public int Score { get; init; }
    public string Label { get; init; } = "Healthy";
    public int DaysRemaining { get; init; }
    /// <summary>Total man-hours logged (formatted as string, e.g. "142.5").</summary>
    public string MilestoneReached { get; init; } = "0";
}
```

### 3b. `GetWorkLogsQueryHandler`

Create in `Application/WorkLogs/Queries/GetWorkLogs/GetWorkLogsQueryHandler.cs`:

```csharp
using MediatR;
using Microsoft.EntityFrameworkCore;
using Aizen.Modules.ServiceRequest.Domain.Entities;

namespace Aizen.Modules.ServiceRequest.Application.WorkLogs.Queries.GetWorkLogs;

public sealed class GetWorkLogsQueryHandler
    : IRequestHandler<GetWorkLogsQuery, GetWorkLogsResult>
{
    private readonly IServiceRequestDbContext _db;   // use the DbContext interface found in Step 0f

    public GetWorkLogsQueryHandler(IServiceRequestDbContext db)
    {
        _db = db;
    }

    public async Task<GetWorkLogsResult> Handle(
        GetWorkLogsQuery request,
        CancellationToken cancellationToken)
    {
        var sr = await _db.ServiceRequests
            .AsNoTracking()
            .Include(x => x.WorkPhases.OrderBy(p => p.DisplayOrder))
            .Include(x => x.WorkLogEntries.OrderByDescending(e => e.Timestamp))
            .FirstOrDefaultAsync(x => x.Id == request.ServiceRequestId, cancellationToken)
            ?? throw new NotFoundException(
                $"ServiceRequest {request.ServiceRequestId} not found.");
        // Use the NotFoundException type already present in the project (Step 0c).

        var phases = sr.WorkPhases
            .OrderBy(p => p.DisplayOrder)
            .Select(p => new WorkPhaseResult
            {
                Number = p.PhaseNumber,
                Title = p.Title,
                ProgressPercent = p.ProgressPercent,
                Status = p.Status,
            })
            .ToList();

        var currentPhase = phases
            .FirstOrDefault(p => p.Status == "In Progress")
            ?? phases.FirstOrDefault();

        var entries = sr.WorkLogEntries
            .OrderByDescending(e => e.Timestamp)
            .Select(e => new WorkLogEntryResult
            {
                Id = e.Id,
                Timestamp = e.Timestamp,
                Type = e.Type,
                Content = e.Content,
                MediaFileId = e.MediaFileId,
                Author = e.Author,
            })
            .ToList();

        // ── Job Health ────────────────────────────────────────────────────────
        // Aggregate man-hours from log entries (future: sum from time-tracking records).
        // For now: count total photo + note entries × 4h as a proxy.
        var totalHours = entries.Count * 4.0;
        var score = Math.Min(100, (int)(totalHours / 2));  // simple proxy until real tracking

        // ── Provider Activity ─────────────────────────────────────────────────
        var providerActivities = entries
            .Take(10)
            .Select((e, i) => new ProviderActivityEntryResult
            {
                Id = e.Id.ToString(),
                Timestamp = e.Timestamp,
                Description = e.Content.Length > 80 ? e.Content[..80] + "…" : e.Content,
                Icon = e.Type switch
                {
                    "photo"         => "photo_camera",
                    "status_change" => "autorenew",
                    _               => "notes",
                },
            })
            .ToList();

        var daysRemaining = sr.RequestedEndDate.HasValue
            ? Math.Max(0, (int)(sr.RequestedEndDate.Value - DateTimeOffset.UtcNow).TotalDays)
            : 0;

        return new GetWorkLogsResult
        {
            ServiceRequestId = sr.Id,
            Title = sr.Title,
            CurrentPhase = currentPhase,
            Phases = phases,
            LogEntries = entries,
            ProviderActivity = new ProviderActivityResult
            {
                ProviderId = sr.AssignedProviderId?.ToString() ?? string.Empty,
                ProviderName = sr.AssignedProviderName ?? string.Empty,
                Activities = providerActivities,
            },
            JobHealth = new JobHealthResult
            {
                Score = score,
                Label = score >= 80 ? "Healthy" : score >= 50 ? "At Risk" : "Critical",
                DaysRemaining = daysRemaining,
                MilestoneReached = totalHours.ToString("0.#"),
            },
        };
    }
}
```

---

## Step 4 — Application Layer: Commands

### 4a. `AddWorkLogEntryCommand`

Create in `Application/WorkLogs/Commands/AddWorkLogEntry/AddWorkLogEntryCommand.cs`:

```csharp
namespace Aizen.Modules.ServiceRequest.Application.WorkLogs.Commands.AddWorkLogEntry;

public sealed record AddWorkLogEntryCommand(
    long ServiceRequestId,
    string Type,
    string Content,
    string Author,
    long? AuthorId = null,
    string? MediaFileId = null
) : IRequest<AddWorkLogEntryResult>;

public sealed record AddWorkLogEntryResult
{
    public long Id { get; init; }
    public DateTimeOffset Timestamp { get; init; }
    public string Type { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? MediaFileId { get; init; }
    public string Author { get; init; } = string.Empty;
}
```

### 4b. `AddWorkLogEntryCommandHandler`

Create in `Application/WorkLogs/Commands/AddWorkLogEntry/AddWorkLogEntryCommandHandler.cs`:

```csharp
namespace Aizen.Modules.ServiceRequest.Application.WorkLogs.Commands.AddWorkLogEntry;

public sealed class AddWorkLogEntryCommandHandler
    : IRequestHandler<AddWorkLogEntryCommand, AddWorkLogEntryResult>
{
    private readonly IServiceRequestDbContext _db;

    public AddWorkLogEntryCommandHandler(IServiceRequestDbContext db)
    {
        _db = db;
    }

    public async Task<AddWorkLogEntryResult> Handle(
        AddWorkLogEntryCommand request,
        CancellationToken cancellationToken)
    {
        var sr = await _db.ServiceRequests
            .Include(x => x.WorkLogEntries)
            .FirstOrDefaultAsync(x => x.Id == request.ServiceRequestId, cancellationToken)
            ?? throw new NotFoundException($"ServiceRequest {request.ServiceRequestId} not found.");

        sr.AddWorkLogEntry(
            type: request.Type,
            content: request.Content,
            author: request.Author,
            authorId: request.AuthorId,
            mediaFileId: request.MediaFileId);

        await _db.SaveChangesAsync(cancellationToken);

        var entry = sr.WorkLogEntries.OrderByDescending(e => e.Timestamp).First();

        return new AddWorkLogEntryResult
        {
            Id = entry.Id,
            Timestamp = entry.Timestamp,
            Type = entry.Type,
            Content = entry.Content,
            MediaFileId = entry.MediaFileId,
            Author = entry.Author,
        };
    }
}
```

### 4c. `UpdateWorkPhaseCommand`

Create in `Application/WorkLogs/Commands/UpdateWorkPhase/UpdateWorkPhaseCommand.cs`:

```csharp
namespace Aizen.Modules.ServiceRequest.Application.WorkLogs.Commands.UpdateWorkPhase;

public sealed record UpdateWorkPhaseCommand(
    long ServiceRequestId,
    int PhaseNumber,
    int ProgressPercent,
    string Status
) : IRequest<UpdateWorkPhaseResult>;

public sealed record UpdateWorkPhaseResult
{
    public int PhaseNumber { get; init; }
    public int ProgressPercent { get; init; }
    public string Status { get; init; } = string.Empty;
}
```

### 4d. `UpdateWorkPhaseCommandHandler`

```csharp
namespace Aizen.Modules.ServiceRequest.Application.WorkLogs.Commands.UpdateWorkPhase;

public sealed class UpdateWorkPhaseCommandHandler
    : IRequestHandler<UpdateWorkPhaseCommand, UpdateWorkPhaseResult>
{
    private readonly IServiceRequestDbContext _db;

    public UpdateWorkPhaseCommandHandler(IServiceRequestDbContext db) => _db = db;

    public async Task<UpdateWorkPhaseResult> Handle(
        UpdateWorkPhaseCommand request,
        CancellationToken cancellationToken)
    {
        var phase = await _db.WorkPhases
            .FirstOrDefaultAsync(
                p => p.ServiceRequestId == request.ServiceRequestId
                  && p.PhaseNumber == request.PhaseNumber,
                cancellationToken)
            ?? throw new NotFoundException(
                $"WorkPhase {request.PhaseNumber} not found on SR {request.ServiceRequestId}.");

        phase.UpdateProgress(request.ProgressPercent, request.Status);
        await _db.SaveChangesAsync(cancellationToken);

        return new UpdateWorkPhaseResult
        {
            PhaseNumber = phase.PhaseNumber,
            ProgressPercent = phase.ProgressPercent,
            Status = phase.Status,
        };
    }
}
```

---

## Step 5 — FluentValidation

Follow the validator style found in Step 0g.

### 5a. `AddWorkLogEntryCommandValidator`

```csharp
using FluentValidation;

namespace Aizen.Modules.ServiceRequest.Application.WorkLogs.Commands.AddWorkLogEntry;

public sealed class AddWorkLogEntryCommandValidator
    : AbstractValidator<AddWorkLogEntryCommand>
{
    private static readonly string[] AllowedTypes = ["note", "photo", "status_change"];

    public AddWorkLogEntryCommandValidator()
    {
        RuleFor(x => x.ServiceRequestId)
            .GreaterThan(0).WithMessage("ServiceRequestId must be a positive integer.");

        RuleFor(x => x.Type)
            .NotEmpty()
            .Must(t => AllowedTypes.Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"Type must be one of: {string.Join(", ", AllowedTypes)}.");

        RuleFor(x => x.Content)
            .NotEmpty()
            .MaximumLength(4000)
            .WithMessage("Content must not be empty and must not exceed 4000 characters.");

        RuleFor(x => x.Author)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.MediaFileId)
            .MaximumLength(256)
            .When(x => x.MediaFileId is not null);
    }
}
```

### 5b. `UpdateWorkPhaseCommandValidator`

```csharp
public sealed class UpdateWorkPhaseCommandValidator
    : AbstractValidator<UpdateWorkPhaseCommand>
{
    private static readonly string[] AllowedStatuses = ["Upcoming", "In Progress", "Completed"];

    public UpdateWorkPhaseCommandValidator()
    {
        RuleFor(x => x.ServiceRequestId).GreaterThan(0);

        RuleFor(x => x.PhaseNumber)
            .InclusiveBetween(1, 20)
            .WithMessage("PhaseNumber must be between 1 and 20.");

        RuleFor(x => x.ProgressPercent)
            .InclusiveBetween(0, 100)
            .WithMessage("ProgressPercent must be 0–100.");

        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(s => AllowedStatuses.Contains(s, StringComparer.Ordinal))
            .WithMessage($"Status must be one of: {string.Join(", ", AllowedStatuses)}.");
    }
}
```

---

## Step 6 — Controller

Add endpoints to the existing `ServiceRequestsController` (or create `WorkLogsController` if the project uses per-resource controllers — follow Step 0d convention).

```csharp
// ── Work Logs ─────────────────────────────────────────────────────────────────

/// <summary>
/// Returns all work-log entries, phase progression, provider activity,
/// and job health for a service request.
/// </summary>
[HttpGet("{id:long}/work-logs")]
[ProducesResponseType(typeof(GetWorkLogsResult), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> GetWorkLogs(
    [FromRoute] long id,
    CancellationToken ct)
{
    var result = await _mediator.Send(new GetWorkLogsQuery(id), ct);
    // Use the same response wrapper found in Step 0d, e.g.:
    return AizenOk(result);   // replace AizenOk with whatever wrapper is in the project
}

/// <summary>
/// Adds a new work-log entry (note, photo reference, or status change).
/// Returns the created entry.
/// </summary>
[HttpPost("{id:long}/work-logs")]
[ProducesResponseType(typeof(AddWorkLogEntryResult), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> AddWorkLogEntry(
    [FromRoute] long id,
    [FromBody] AddWorkLogEntryRequest body,
    CancellationToken ct)
{
    var command = new AddWorkLogEntryCommand(
        ServiceRequestId: id,
        Type: body.Type,
        Content: body.Content,
        Author: body.Author,
        AuthorId: body.AuthorId,
        MediaFileId: body.MediaFileId);

    var result = await _mediator.Send(command, ct);
    return CreatedAtAction(nameof(GetWorkLogs), new { id }, result);
}

/// <summary>
/// Updates the progress and status of a work phase.
/// Admin/Provider only.
/// </summary>
[HttpPatch("{id:long}/work-logs/phases/{phaseNumber:int}")]
[ProducesResponseType(typeof(UpdateWorkPhaseResult), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public async Task<IActionResult> UpdateWorkPhase(
    [FromRoute] long id,
    [FromRoute] int phaseNumber,
    [FromBody] UpdateWorkPhaseRequest body,
    CancellationToken ct)
{
    var command = new UpdateWorkPhaseCommand(
        ServiceRequestId: id,
        PhaseNumber: phaseNumber,
        ProgressPercent: body.ProgressPercent,
        Status: body.Status);

    var result = await _mediator.Send(command, ct);
    return AizenOk(result);
}
```

### Request models (in the API layer, or Application layer if already pattern established)

```csharp
public sealed record AddWorkLogEntryRequest
{
    /// <summary>note | photo | status_change</summary>
    public string Type { get; init; } = "note";
    public string Content { get; init; } = string.Empty;
    public string Author { get; init; } = string.Empty;
    public long? AuthorId { get; init; }
    public string? MediaFileId { get; init; }
}

public sealed record UpdateWorkPhaseRequest
{
    public int ProgressPercent { get; init; }
    /// <summary>Upcoming | In Progress | Completed</summary>
    public string Status { get; init; } = string.Empty;
}
```

---

## Step 7 — EF Migration

```bash
# From the infrastructure project directory:
dotnet ef migrations add AddWorkLogs \
  --project <InfrastructureProject> \
  --startup-project <ApiProject>
```

Review the generated migration. Confirm:
- `work_log_entries` table created with all columns
- `work_phases` table created with all columns
- Foreign key constraints to `service_requests` with CASCADE DELETE
- Unique index on `(service_request_id, phase_number)` in `work_phases`
- Indexes on `service_request_id` for both tables
- No unintended changes to existing tables

```bash
dotnet ef database update
```

---

## Step 8 — Final Checklist

- [ ] Step 0 reads completed before any file written
- [ ] `WorkLogEntryEntity.Create()` factory validates type + content
- [ ] `WorkPhaseEntity.UpdateProgress()` clamps 0–100 and validates status
- [ ] DbSets registered in DbContext
- [ ] Both entity configurations registered in `OnModelCreating` (via assembly scan or explicit call)
- [ ] `GetWorkLogsQuery` includes phases ordered by `DisplayOrder`, entries ordered by `Timestamp DESC`
- [ ] `NotFoundException` is the project's existing type — NOT a new custom exception
- [ ] `AddWorkLogEntryCommandValidator` registered (assembly scan or explicit)
- [ ] `UpdateWorkPhaseCommandValidator` registered
- [ ] Controller endpoints compile — no `AizenOk` placeholder left (replaced with project's actual wrapper)
- [ ] Migration reviewed and applied
- [ ] `GET /api/service-requests/{id}/work-logs` returns 200 with phases + entries
- [ ] `POST /api/service-requests/{id}/work-logs` returns 201 with the created entry
- [ ] `PATCH /api/service-requests/{id}/work-logs/phases/{n}` returns 200 with updated phase
- [ ] All three endpoints return 404 when the service request does not exist
