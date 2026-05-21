# 07 - Wire Operation Application Pipeline

This step wires authentication and authorization middleware through the operation application configuration.

## Target

Inspect and update if required:

```text
Aizen.Core.Starter.Operation
AizenOperationApplicationConfiguration
```

## Goal

Ensure every operation API application uses the authentication and authorization middleware in the correct order.

## Required Middleware

The effective pipeline must include:

```csharp
app.UseAuthentication();
app.UseAuthorization();
```

They must run before controller endpoint execution.

## Required Order

The expected effective order should be equivalent to:

```csharp
app.UseRouting();

app.UseCors(...); // if used

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
```

The project may use minimal hosting or extension wrappers. Preserve existing structure.

## Controller Mapping

If the framework maps controllers centrally, consider whether this should become:

```csharp
app.MapControllers().RequireAuthorization();
```

However, if the fallback policy is properly configured in `Aizen.Core.Auth`, this may not be necessary.

Use one consistent strategy.

Preferred:

- Central fallback/default policy in `Aizen.Core.Auth`
- Pipeline calls in operation starter
- Public endpoints opt out with `[AllowAnonymous]`

## Do Not Break

Do not break:

- Swagger
- Health checks
- CORS
- Exception handling
- Logging
- Localization
- Rate limiting
- Existing endpoint mappings

## Required Output

Produce:

```md
# Operation Application Pipeline Result

## UseAuthentication
- Added / Already exists

## UseAuthorization
- Added / Already exists

## Middleware Order
1. ...
2. ...
3. ...

## Controller Mapping
- ...

## Files Changed
- ...

## Reasoning
- ...
```
