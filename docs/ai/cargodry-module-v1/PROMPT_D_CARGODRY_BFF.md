# PROMPT D — CargoDry BFF Integration

## Context

Admin Panel BFF (`Aizen.AdminPanel.Bff`) içindeki CargoDry entegrasyonu.
Mevcut durumda `AdminInactiveModulesController`'da 501 dönen iki endpoint var:
- `GET /cargodry/kits` → 501
- `POST /cargodry/kits/activate` → 501

`GetAdminVesselDetailBffQueryHandler`'da comment: "CargoDry module not yet integrated"

Bu dosyada tüm bu stub'lar gerçek implementasyona çevrilecek.
Onboarding flow BFF endpoint'leri de burada tanımlanır.

BFF namespace convention: `Aizen.AdminPanel.Bff`
Existing pattern: Refit interface → Remote call → BFF Query/Command Handler → Controller

---

## STEP 1 — Update CargoDryKitBffDto

Mevcut `CargoDryKitBffDto`'yu güncelle. Tüm alanlar eklenecek.

**`Dtos/CargoDry/CargoDryKitBffDto.cs`**
```csharp
namespace Aizen.AdminPanel.Bff.Dtos.CargoDry;

public sealed class CargoDryKitBffDto
{
    public long     Id                { get; init; }
    public string   SerialNumber      { get; init; } = default!;
    public string   KitCode           { get; init; } = default!;
    public string   ProductCode       { get; init; } = default!;
    public string   ProductName       { get; init; } = default!;
    public string   BatchCode         { get; init; } = default!;
    public string   Status            { get; init; } = default!;
    public long?    OwnerUserId       { get; init; }
    public string?  OwnerDisplayName  { get; init; }
    public long?    VesselId          { get; init; }
    public string?  VesselName        { get; init; }
    public string?  ActivatedDate     { get; init; }  // ISO string, UI'da format edilir
    public string?  ExpiryDate        { get; init; }
    public double   EfficiencyPercent { get; init; }
    public int      DaysUntilExpiry   { get; init; }
    public int      RenewalCount      { get; init; }
    public string   ManufacturedAt    { get; init; } = default!;
}

public sealed class CargoDryKitListBffDto
{
    public List<CargoDryKitBffDto> Items    { get; init; } = [];
    public int                      Total    { get; init; }
    public int                      Page     { get; init; }
    public int                      PageSize { get; init; }
}

public sealed class CargoDryStatsBffDto
{
    public int    TotalKits            { get; init; }
    public int    AvailableKits        { get; init; }
    public int    ActiveKits           { get; init; }
    public int    ExpiringKits         { get; init; }
    public int    ExpiredKits          { get; init; }
    public int    RevokedKits          { get; init; }
    public int    TodayActivations     { get; init; }
    public int    TotalBatches         { get; init; }
    public double RenewalRatePercent   { get; init; }
}

public sealed class CargoDryValidationBffDto
{
    public bool    IsValid          { get; init; }
    public string? InvalidReason    { get; init; }
    public string? ProductName      { get; init; }
    public string? ProductCode      { get; init; }
    public int     ValidityDays     { get; init; }
    public bool    HasSmartDevice   { get; init; }
    public string? ActivationToken  { get; init; }
    public string? TokenExpiresAt   { get; init; }
}

public sealed class GenerateBatchBffResultDto
{
    public string BatchCode      { get; init; } = default!;
    public int    GeneratedCount { get; init; }
    public string QrZipFileUrl   { get; init; } = default!;
    public string ExcelFileUrl   { get; init; } = default!;
}
```

---

## STEP 2 — Refit Remote Call Interface

**`RemoteCalls/ICargoDryBffRemoteCall.cs`**
```csharp
using Aizen.AdminPanel.Bff.Dtos.CargoDry;
using Refit;

namespace Aizen.AdminPanel.Bff.RemoteCalls;

public interface IAdminCargoDryBffRemoteCall
{
    // ── Kit Management ────────────────────────────────────────────────────────

    [Get("/api/v1/cargodry/admin/kits")]
    Task<CargoDryKitListBffDto> GetKitsAsync(
        [Query] string? status = null,
        [Query] string? search = null,
        [Query] int page = 1,
        [Query] int pageSize = 25,
        CancellationToken ct = default);

    [Get("/api/v1/cargodry/admin/stats")]
    Task<CargoDryStatsBffDto> GetStatsAsync(CancellationToken ct = default);

    [Post("/api/v1/cargodry/admin/batches/generate")]
    Task<GenerateBatchBffResultDto> GenerateBatchAsync(
        [Body] GenerateBatchBffRequest request, CancellationToken ct = default);

    [Post("/api/v1/cargodry/admin/kits/{id}/revoke")]
    Task<bool> RevokeKitAsync(long id, [Body] RevokeKitBffRequest request, CancellationToken ct = default);

    [Post("/api/v1/cargodry/admin/kits/{id}/extend")]
    Task<CargoDryKitBffDto> ExtendKitAsync(long id, [Body] ExtendKitBffRequest request, CancellationToken ct = default);

    // ── Vessel Integration ────────────────────────────────────────────────────

    [Get("/api/v1/cargodry/kits")]
    Task<List<CargoDryKitBffDto>> GetKitsByOwnerAsync(
        [Query] long userId, CancellationToken ct = default);

    // ── Onboarding (Public — no admin auth) ──────────────────────────────────

    [Post("/api/v1/cargodry/public/validate")]
    Task<CargoDryValidationBffDto> ValidateKitAsync(
        [Body] ValidateKitBffRequest request, CancellationToken ct = default);

    [Post("/api/v1/cargodry/kits/activate")]
    Task<CargoDryKitBffDto> ActivateKitAsync(
        [Body] ActivateKitBffRequest request, CancellationToken ct = default);
}

// ── Request DTOs ─────────────────────────────────────────────────────────────

public sealed class GenerateBatchBffRequest
{
    public string ProductCode { get; init; } = default!;
    public int    Count       { get; init; }
}

public sealed class RevokeKitBffRequest
{
    public string Reason { get; init; } = default!;
}

public sealed class ExtendKitBffRequest
{
    public int AddedDays { get; init; }
}

public sealed class ValidateKitBffRequest
{
    public string  SerialNumber { get; init; } = default!;
    public string  BatchCode    { get; init; } = default!;
    public string? Signature    { get; init; }
}

public sealed class ActivateKitBffRequest
{
    public string ActivationToken { get; init; } = default!;
    public long   VesselId        { get; init; }
}
```

---

## STEP 3 — BFF Controllers (Replace 501 stubs)

### 3.1 Admin CargoDry Controller

Find `AdminInactiveModulesController` and **replace** CargoDry endpoints.
Create new dedicated controller:

**`Controllers/AdminCargoDryController.cs`**
```csharp
using Aizen.AdminPanel.Bff.Dtos.CargoDry;
using Aizen.AdminPanel.Bff.RemoteCalls;
using Aizen.Core.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.AdminPanel.Bff.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
[Route("api/v1/admin-panel/cargodry")]
public sealed class AdminCargoDryController : AizenBaseController
{
    private readonly IAdminCargoDryBffRemoteCall _cargoDry;
    public AdminCargoDryController(IAdminCargoDryBffRemoteCall cargoDry) => _cargoDry = cargoDry;

    /// <summary>GET /api/v1/admin-panel/cargodry/kits</summary>
    [HttpGet("kits")]
    public async Task<IActionResult> GetKits(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var result = await _cargoDry.GetKitsAsync(status, search, page, pageSize, ct);
        return Ok(result);
    }

    /// <summary>GET /api/v1/admin-panel/cargodry/stats</summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(CancellationToken ct)
    {
        var result = await _cargoDry.GetStatsAsync(ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/batches/generate</summary>
    [HttpPost("batches/generate")]
    public async Task<IActionResult> GenerateBatch(
        [FromBody] GenerateBatchBffRequest request, CancellationToken ct)
    {
        var result = await _cargoDry.GenerateBatchAsync(request, ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/kits/{id}/revoke</summary>
    [HttpPost("kits/{id:long}/revoke")]
    public async Task<IActionResult> RevokeKit(
        long id, [FromBody] RevokeKitBffRequest request, CancellationToken ct)
    {
        var result = await _cargoDry.RevokeKitAsync(id, request, ct);
        return Ok(result);
    }

    /// <summary>POST /api/v1/admin-panel/cargodry/kits/{id}/extend</summary>
    [HttpPost("kits/{id:long}/extend")]
    public async Task<IActionResult> ExtendKit(
        long id, [FromBody] ExtendKitBffRequest request, CancellationToken ct)
    {
        var result = await _cargoDry.ExtendKitAsync(id, request, ct);
        return Ok(result);
    }
}
```

### 3.2 Onboarding BFF Controller

Bu endpoint'ler mobil/web onboarding flow için BFF tarafından proxy'lenir.
BFF katmanı auth token'ı yönetir ve CargoDry servisine yönlendirir.

**`Controllers/CargoDryOnboardingController.cs`**
```csharp
using Aizen.AdminPanel.Bff.Dtos.CargoDry;
using Aizen.AdminPanel.Bff.RemoteCalls;
using Aizen.Core.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Aizen.AdminPanel.Bff.Controllers;

[Route("api/v1/onboarding/cargodry")]
public sealed class CargoDryOnboardingController : AizenBaseController
{
    private readonly IAdminCargoDryBffRemoteCall _cargoDry;
    public CargoDryOnboardingController(IAdminCargoDryBffRemoteCall cargoDry) => _cargoDry = cargoDry;

    /// <summary>
    /// POST /api/v1/onboarding/cargodry/validate
    /// [AllowAnonymous] — kullanıcı henüz login olmadan QR tarayabilir.
    /// Dönen ActivationToken 5 dakika geçerlidir.
    /// Frontend bu token'ı saklar, login sonrası activate'e gönderir.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("validate")]
    public async Task<IActionResult> Validate(
        [FromBody] ValidateKitBffRequest request, CancellationToken ct)
    {
        var result = await _cargoDry.ValidateKitAsync(request, ct);
        return Ok(result);
    }

    /// <summary>
    /// POST /api/v1/onboarding/cargodry/activate
    /// [Authorize] — kullanıcı login olduktan sonra aktivasyon tamamlanır.
    /// Body: { activationToken (5-min JWT), vesselId }
    /// </summary>
    [Authorize]
    [HttpPost("activate")]
    public async Task<IActionResult> Activate(
        [FromBody] ActivateKitBffRequest request, CancellationToken ct)
    {
        var result = await _cargoDry.ActivateKitAsync(request, ct);
        return Ok(result);
    }
}
```

---

## STEP 4 — Vessel Detail Integration

Find `GetAdminVesselDetailBffQueryHandler` and update the CargoDry section.

```csharp
// In GetAdminVesselDetailBffQueryHandler.Handle():
// BEFORE (stub comment):
// // CargoDry module not yet integrated
// CargoDryKit = null

// AFTER:
List<CargoDryKitBffDto> cargoDryKits = [];
try
{
    cargoDryKits = await _cargoDry.GetKitsByOwnerAsync(vessel.OwnerUserId, ct);
    // Filter to only kits linked to this vessel
    cargoDryKits = cargoDryKits
        .Where(k => k.VesselId == request.VesselId)
        .ToList();
}
catch (Exception ex)
{
    _logger.LogWarning(ex, "CargoDry kit fetch failed for vessel {VesselId}", request.VesselId);
}

// Add to VesselDetailBffDto:
CargoDryKits = cargoDryKits,
```

**Update `VesselDetailBffDto`** to include:
```csharp
public List<CargoDryKitBffDto> CargoDryKits { get; init; } = [];
```

---

## STEP 5 — BFF DI Registration

In BFF `Program.cs` veya `DependencyInjection.cs`:
```csharp
builder.Services.AddRefitClient<IAdminCargoDryBffRemoteCall>()
    .ConfigureHttpClient(c =>
        c.BaseAddress = new Uri(builder.Configuration["Services:CargoDry"]!));
```

`appsettings.json` BFF:
```json
{
  "Services": {
    "CargoDry": "https://localhost:7XXX"
  }
}
```

---

## STEP 6 — Onboarding Flow API Contract

Bu section frontend/mobile için flow dokümantasyonu. Backend kodu değil.

### Onboarding Senaryosu A: QR Tarama → Yeni Kullanıcı

```
1. Kullanıcı QR'ı tarar (login yok)
   POST /api/v1/onboarding/cargodry/validate
   Body: { serialNumber, batchCode, signature }
   → Response: { isValid: true, productName, validityDays, activationToken, tokenExpiresAt }
   → Frontend: activationToken'ı sessionStorage'a kaydeder (5 dk TTL)

2. Onboarding: Kayıt ol veya giriş yap
   POST /api/v1/identity/participant/register
   Body: { email, phone, password, firstName, lastName, kvkkAccepted,
           deviceId, deviceType, notificationToken }
   → Response: { accessToken, refreshToken, userId }

3. Gemi seç veya ekle
   GET  /api/v1/vessel/vessels → Mevcut gemiler
   POST /api/v1/vessel/vessels → Yeni gemi ekle (name, type, registration, year, imo...)
   → Response: { vesselId }

4. Kiti aktive et (artık login olundu)
   POST /api/v1/onboarding/cargodry/activate
   Headers: Authorization: Bearer {accessToken}
   Body: { activationToken, vesselId }
   → Response: CargoDryKitDto (kitCode, expiresAt, efficiencyPercent...)

5. Onboarding tamamlandı → Dashboard'a yönlendir
```

### Onboarding Senaryosu B: Mevcut Kullanıcı

```
1. QR Tarama → validate (activationToken al)
2. Zaten login → activate (vesselId seç)
```

### Onboarding Senaryosu C: Manuel Seri No Girişi

```
POST /api/v1/onboarding/cargodry/validate
Body: { serialNumber: "ABCD-EFGH-JKLM-NOPQ", batchCode: "202506-STAN-AB12", signature: null }
→ Signature null → HMAC kontrolü atlanır, sadece seri no + batch varlık kontrolü
```

---

## STEP 7 — AdminInactiveModulesController Cleanup

Find `AdminInactiveModulesController`. Remove or comment out these stubs:
```csharp
// DELETE or replace:
[HttpGet("cargodry/kits")]
public IActionResult GetCargoDryKits() => StatusCode(501);

[HttpPost("cargodry/kits/activate")]
public IActionResult ActivateCargoDryKit() => StatusCode(501);
```

These are now handled by `AdminCargoDryController` and `CargoDryOnboardingController`.

---

## Verification Checklist

- [ ] `GET /api/v1/admin-panel/cargodry/kits` → 200 (was 501)
- [ ] `POST /api/v1/admin-panel/cargodry/batches/generate` → 200 with BatchCode + file URLs
- [ ] `POST /api/v1/onboarding/cargodry/validate` → `[AllowAnonymous]`, no auth header needed
- [ ] `POST /api/v1/onboarding/cargodry/activate` → `[Authorize]`, 401 without token
- [ ] Vessel detail response includes `cargoDryKits` array
- [ ] BFF Refit client base URL configured from `Services:CargoDry`
- [ ] `AdminInactiveModulesController` CargoDry stubs removed
