# 10 - Swagger, Health Check and Development Exceptions

This step verifies that infrastructure endpoints still work with the new default protection.

## Swagger

If Swagger/OpenAPI is configured in the framework, preserve the existing location.

Do not scatter Swagger configuration into service `Program.cs` if the framework already has a Swagger starter.

Ensure Swagger supports Bearer JWT input.

If missing, add a Bearer security definition in the existing Swagger/OpenAPI configuration location.

Reference shape:

```csharp
options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
{
    Description = "JWT Authorization header using the Bearer scheme. Example: Bearer {token}",
    Name = "Authorization",
    In = ParameterLocation.Header,
    Type = SecuritySchemeType.Http,
    Scheme = "bearer",
    BearerFormat = "JWT"
});
```

and security requirement referencing `"Bearer"`.

## Health Checks

If health checks are mapped centrally, keep them public if that is the existing intended behavior.

Examples:

```csharp
app.MapHealthChecks("/health").AllowAnonymous();
app.MapHealthChecks("/healthz").AllowAnonymous();
app.MapHealthChecks("/ready").AllowAnonymous();
app.MapHealthChecks("/live").AllowAnonymous();
```

Only apply routes that actually exist.

## Development Exceptions

Do not make Swagger publicly available in production if the existing project intentionally limits it by environment.

Preserve current environment checks.

## Required Output

Produce:

```md
# Swagger and Health Result

## Swagger Bearer Support
- Already exists / Added

## Health Check Public Access
- Already exists / Added

## Environment Rules Preserved
- ...

## Files Changed
- ...
```
