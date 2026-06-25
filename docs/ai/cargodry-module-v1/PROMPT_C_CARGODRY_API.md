# PROMPT C — CargoDry Module: API Layer (Program.cs + Controllers)

## Context

`Aizen.Modules.CargoDry` host projesidir. Mevcut Program.cs weather forecast template'i tamamen silinecek.
`AizenApplicationBuilder.CreateBuilder` ile `AppType.Operation` olarak ayarlanacak.
IP Rate Limiting, Redis ile sliding window algoritmasıyla `/validate` endpoint'ini korur.

---

## STEP 1 — Program.cs (Full Replacement)

**`Program.cs`**
```csharp
using Aizen.Core.Application;
using Aizen.Core.Application.Enums;
using Aizen.Modules.CargoDry.Application;
using Aizen.Modules.CargoDry.Application.Consumers;
using Aizen.Modules.CargoDry.Repository;
using Aizen.Modules.CargoDry.Repository.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;

var builder = AizenApplicationBuilder.CreateBuilder(new AizenAppInfo
{
    Name        = "CargoDry",
    Type        = AppType.Operation,
    TypeInclude = { AppType.Api, AppType.Worker, AppType.Scheduler },
}, args);

// ── Repository ────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<CargoDryDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("CargoDry")));
builder.Services.AddCargoDryRepository();

// ── Application ───────────────────────────────────────────────────────────────
builder.Services.AddCargoDryApplication();
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(
        typeof(Aizen.Modules.CargoDry.Application.DependencyInjection).Assembly));

// ── Redis (Rate limiting + Activation token JTI store) ───────────────────────
builder.Services.AddStackExchangeRedisCache(opts =>
    opts.Configuration = builder.Configuration.GetConnectionString("Redis"));
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ =>
    StackExchange.Redis.ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis")!));

// ── Rate Limiting (IP-based, 10 req/min for validate endpoint) ───────────────
builder.Services.AddRateLimiter(opts =>
{
    opts.AddSlidingWindowLimiter("validate-ip", limiter =>
    {
        limiter.PermitLimit         = 10;
        limiter.Window              = TimeSpan.FromMinutes(1);
        limiter.SegmentsPerWindow   = 6;  // 10-second segments
        limiter.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiter.QueueLimit          = 0;
    });
    opts.RejectionStatusCode = 429;
});

// ── MassTransit ───────────────────────────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<CommerceOrderCompletedConsumer>();
    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"], h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"]!);
            h.Password(builder.Configuration["RabbitMq:Password"]!);
        });
        cfg.ConfigureEndpoints(ctx);
    });
});

var app = builder.Build();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ── Seed ──────────────────────────────────────────────────────────────────────
await app.SeedCargoDryAsync();

app.Run();
```

---

## STEP 2 — Controllers

### 2.1 Public Validate Controller (No Auth — QR scan pre-login)

**`Controllers/CargoDryPublicController.cs`**
```csharp
using Aizen.Core.Api.Controllers;
using Aizen.Modules.CargoDry.Application.Commands.ValidateKit;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Aizen.Modules.CargoDry.Controllers;

/// <summary>
/// Public endpoint — authentication gerekmez.
/// QR kodu tarandığında mobil uygulama bu endpoint'i çağırır.
/// Rate limiter: 10 req/min/IP (brute-force koruması).
/// </summary>
[AllowAnonymous]
[Route("api/v1/cargodry/public")]
public sealed class CargoDryPublicController : AizenBaseController
{
    private readonly IMediator _mediator;
    public CargoDryPublicController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// QR veya seri numarasını doğrula ve 5 dakikalık activation token üret.
    /// POST /api/v1/cargodry/public/validate
    /// </summary>
    [HttpPost("validate")]
    [EnableRateLimiting("validate-ip")]
    public async Task<IActionResult> ValidateKit(
        [FromBody] ValidateKitRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ValidateKitCommand
        {
            SerialNumber = request.SerialNumber,
            BatchCode    = request.BatchCode,
            Signature    = request.Signature,
            Source       = ActivationSource.MobileApp,
        }, ct);
        return Ok(result);
    }
}

public sealed class ValidateKitRequest
{
    public string  SerialNumber { get; init; } = default!;
    public string  BatchCode    { get; init; } = default!;
    public string? Signature    { get; init; }  // HMAC — null for serial-only flow
}
```

---

### 2.2 Kit Controller (Authenticated — user owns the kit)

**`Controllers/CargoDryKitsController.cs`**
```csharp
using Aizen.Core.Api.Controllers;
using Aizen.Modules.CargoDry.Application.Commands.ActivateKit;
using Aizen.Modules.CargoDry.Application.Commands.RenewKit;
using Aizen.Modules.CargoDry.Application.Queries.GetMyKits;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.CargoDry.Controllers;

[Authorize]
[Route("api/v1/cargodry/kits")]
public sealed class CargoDryKitsController : AizenBaseController
{
    private readonly IMediator _mediator;
    public CargoDryKitsController(IMediator mediator) => _mediator = mediator;

    /// <summary>GET /api/v1/cargodry/kits — kullanıcının kitlerini listele</summary>
    [HttpGet]
    public async Task<IActionResult> GetMyKits(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new GetMyKitsQuery { UserId = userId }, ct);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/cargodry/kits/activate
    /// Body: { activationToken, vesselId }
    /// activationToken: validate endpoint'ten alınan 5 dakikalık JWT
    /// </summary>
    [HttpPost("activate")]
    public async Task<IActionResult> ActivateKit(
        [FromBody] ActivateKitRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ActivateKitCommand
        {
            ActivationToken = request.ActivationToken,
            VesselId        = request.VesselId,
            UserId          = GetCurrentUserId(),
            Method          = request.Method,
            Source          = ActivationSource.MobileApp,
            IpAddress       = HttpContext.Connection.RemoteIpAddress?.ToString(),
            DeviceInfo      = Request.Headers.UserAgent.ToString(),
        }, ct);
        return Ok(result);
    }
}

public sealed class ActivateKitRequest
{
    public string          ActivationToken { get; init; } = default!;
    public long            VesselId        { get; init; }
    public ActivationMethod Method         { get; init; } = ActivationMethod.QrScan;
}
```

---

### 2.3 Admin Controller

**`Controllers/CargoDryAdminController.cs`**
```csharp
using Aizen.Core.Api.Controllers;
using Aizen.Modules.CargoDry.Application.Commands.GenerateBatch;
using Aizen.Modules.CargoDry.Application.Commands.RevokeKit;
using Aizen.Modules.CargoDry.Application.Commands.RenewKit;
using Aizen.Modules.CargoDry.Application.Queries.GetAdminKitList;
using Aizen.Modules.CargoDry.Application.Queries.GetCargoDryStats;
using Aizen.Modules.CargoDry.Abstraction.Enum;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.Modules.CargoDry.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/cargodry/admin")]
public sealed class CargoDryAdminController : AizenBaseController
{
    private readonly IMediator _mediator;
    public CargoDryAdminController(IMediator mediator) => _mediator = mediator;

    /// <summary>GET /api/v1/cargodry/admin/kits?status=Activated&search=CD-&page=1&pageSize=25</summary>
    [HttpGet("kits")]
    public async Task<IActionResult> GetKits(
        [FromQuery] CargoDryKitStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetAdminKitListQuery
        {
            Status   = status,
            Search   = search,
            Page     = page,
            PageSize = pageSize,
        }, ct);
        return Ok(result);
    }

    /// <summary>GET /api/v1/cargodry/admin/stats</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await _mediator.Send(new GetCargoDryStatsQuery(), ct);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/cargodry/admin/batches/generate
    /// Body: { productCode, count }
    /// </summary>
    [HttpPost("batches/generate")]
    public async Task<IActionResult> GenerateBatch(
        [FromBody] GenerateBatchRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new GenerateBatchCommand
        {
            ProductCode = request.ProductCode,
            Count       = request.Count,
            AdminUserId = GetCurrentUserId(),
        }, ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/cargodry/admin/kits/{id}/revoke</summary>
    [HttpPost("kits/{id:long}/revoke")]
    public async Task<IActionResult> RevokeKit(
        long id, [FromBody] RevokeKitRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RevokeKitCommand
        {
            KitId       = id,
            Reason      = request.Reason,
            AdminUserId = GetCurrentUserId(),
        }, ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/cargodry/admin/kits/{id}/extend — admin manuel süre uzatma</summary>
    [HttpPost("kits/{id:long}/extend")]
    public async Task<IActionResult> ExtendKit(
        long id, [FromBody] ExtendKitRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RenewKitCommand
        {
            KitId       = id,
            AddedDays   = request.AddedDays,
            Type        = RenewalType.AdminExtension,
            AdminUserId = GetCurrentUserId(),
        }, ct);
        return Ok(result);
    }
}

public sealed class GenerateBatchRequest
{
    public string ProductCode { get; init; } = default!;
    public int    Count       { get; init; }
}

public sealed class RevokeKitRequest
{
    public string Reason { get; init; } = default!;
}

public sealed class ExtendKitRequest
{
    public int AddedDays { get; init; }
}
```

---

## STEP 3 — appsettings.json

**`appsettings.json`** (add these sections):
```json
{
  "ConnectionStrings": {
    "CargoDry": "Host=localhost;Port=5432;Database=inktavia;Username=postgres;Password=xxx;Search Path=cargodry",
    "Redis": "localhost:6379"
  },
  "CargoDry": {
    "ActivationTokenSecret": "REPLACE_WITH_32_CHAR_SECRET_MINIMUM",
    "BatchKeys": {
      "202506-STAN-AB12": "REPLACE_WITH_BASE64_BATCH_KEY"
    }
  },
  "RabbitMq": {
    "Host": "amqp://localhost",
    "Username": "guest",
    "Password": "guest"
  }
}
```

---

## Verification Checklist

- [ ] `POST /api/v1/cargodry/public/validate` — `[AllowAnonymous]`, rate limiter aktif, HTTP 429 döner (11. istek)
- [ ] `POST /api/v1/cargodry/kits/activate` — `[Authorize]`, UserId JWT'den alınıyor
- [ ] `POST /api/v1/cargodry/admin/batches/generate` — `[Authorize(Roles="Admin")]`, 5000 limit validator
- [ ] `POST /api/v1/cargodry/admin/kits/{id}/revoke` — integration event yayınlandı
- [ ] Program.cs Redis bağlantısı var, DbContext "cargodry" schema
- [ ] `dotnet build Aizen.Modules.CargoDry` — zero errors
- [ ] Migration: `dotnet ef migrations add InitialCreate --project .../Repository --startup-project ... --context CargoDryDbContext`
