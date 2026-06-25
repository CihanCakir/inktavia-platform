# AdminPanel BFF Keycloak Service Token Options Validation Report

## Report Date
2026-06-10

---

## Changes to `KeycloakServiceTokenOptions`

Added `[Required]` validation annotations to all mandatory fields:

```csharp
public sealed class KeycloakServiceTokenOptions
{
    public const string SectionName = "KeycloakServiceToken";

    [Required(ErrorMessage = "KeycloakServiceToken:Authority is required.")]
    public string Authority { get; set; } = default!;

    [Required(ErrorMessage = "KeycloakServiceToken:TokenEndpoint is required.")]
    public string TokenEndpoint { get; set; } = default!;

    [Required(ErrorMessage = "KeycloakServiceToken:ClientId is required.")]
    public string ClientId { get; set; } = default!;

    [Required(ErrorMessage = "KeycloakServiceToken:ClientSecret is required.")]
    public string ClientSecret { get; set; } = default!;

    public int CacheSecondsBeforeExpiry { get; set; } = 60;

    [Required(ErrorMessage = "KeycloakServiceToken:CacheKeyPrefix is required.")]
    public string CacheKeyPrefix { get; set; } = "inktavia:admin-panel-bff:keycloak-service-token";
}
```

---

## DI Registration Updated to Use `ValidateOnStart`

Changed from `services.Configure<T>()` to `services.AddOptions<T>().Bind().ValidateDataAnnotations().ValidateOnStart()`:

```csharp
services.AddOptions<KeycloakServiceTokenOptions>()
    .Bind(configuration.GetSection(KeycloakServiceTokenOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

**Effect:** If any `[Required]` field is missing or null at application startup, the app fails immediately with a clear `OptionsValidationException` rather than failing silently on the first token request.

---

## Required Configuration Fields

| Field | Source | Validated |
|---|---|---|
| `Authority` | `KeycloakServiceToken:Authority` env var or appsettings | ✅ `[Required]` |
| `TokenEndpoint` | `KeycloakServiceToken:TokenEndpoint` env var or appsettings | ✅ `[Required]` |
| `ClientId` | `KeycloakServiceToken:ClientId` — defaults to `"admin-panel-bff"` | ✅ `[Required]` |
| `ClientSecret` | `KeycloakServiceToken__ClientSecret` env var | ✅ `[Required]` |
| `CacheSecondsBeforeExpiry` | appsettings — defaults to `60` | No validation needed (has default) |
| `CacheKeyPrefix` | appsettings — defaults to `"inktavia:admin-panel-bff:keycloak-service-token"` | ✅ `[Required]` |

---

## Secret Injection

`ClientSecret` must NEVER be committed. Inject via:

| Method | Format |
|---|---|
| Environment variable | `KeycloakServiceToken__ClientSecret=<value>` |
| Docker secret / Kubernetes secret | Mount as env var |
| .NET User Secrets (dev) | `dotnet user-secrets set "KeycloakServiceToken:ClientSecret" "<value>"` |

---

## Placeholder Sentinel Value Note

The appsettings files use `"__FROM_SECRET__"` as a sentinel for the `ClientSecret`. `[Required]` validates that the string is not `null` or empty — it does NOT reject `"__FROM_SECRET__"` as a sentinel value.

If the app is deployed without overriding the sentinel value, Keycloak will reject the `client_credentials` request with a 401. The startup validation catches null/empty; sentinel-value detection would require a custom `IValidateOptions<T>` implementation.

**Follow-up:** Add a custom `IValidateOptions<KeycloakServiceTokenOptions>` that rejects `"__FROM_SECRET__"` and `"__FROM_ENV__"` in non-Development environments.

---

## `DistributedCache:Configuration` Validation

The `AddAizenCache(configuration)` call in `DependencyInjection.cs` reads `DistributedCache:Configuration` directly via `Connection.RedisConnection = configuration["DistributedCache:Configuration"]`. If this is null/empty, `AizenDistributedCache` will fail to connect to Redis at startup.

**No startup validation** currently exists for this value in the Aizen cache layer.

**Follow-up:** Add validation for `DistributedCache:Configuration` using a similar options pattern or via an `IStartupFilter`.

---

## Validation Results

Build: `dotnet build Bff/src/AdminPanel/Aizen.Bff.AdminPanel/Aizen.Bff.AdminPanel.csproj`
Result: ✅ **0 errors** — compilation succeeds with all validation attributes.

Tests: No test projects exist for the AdminPanel BFF.
