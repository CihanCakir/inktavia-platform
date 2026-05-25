# 04 - Discovery: CQRS / Handler Scope

Middleware may set a scoped container, but handlers may be resolved in a different scope.

Inspect CQRS/mediator infrastructure:

```text
Core/CQRS
Core/Mediator
Core/Application
CommandHandler
QueryHandler
Send
Dispatch
IServiceProvider.CreateScope
CreateAsyncScope
IMediator
MediatR
PipelineBehavior
UnitOfWork
```

## Questions

- How are controllers dispatching commands/queries?
- Does command/query dispatch create a new `IServiceScope`?
- Does any pipeline behavior create a new scope?
- Does UnitOfWork create a new scope?
- Are handlers resolved from root provider, request provider, or child provider?
- Is `_infoAccessor` resolved in the same request scope as middleware?

## If a Child Scope Exists

Possible fixes:

1. Avoid creating a new scope for request command/query dispatch.
2. Propagate AizenInfo values to the child scope.
3. Make `UserInfoAccessor` read from `HttpContext.Items` via `IHttpContextAccessor`.
4. Use existing AsyncLocal request context if project already has one.

Prefer the least invasive architecture-compatible fix.

## Output Required

```md
# Handler Scope Report

## Dispatch Path
- ...

## Scope Creation Points
- ...

## Handler Lifetime
- ...

## Same Scope As Middleware?
- Yes/No

## Scope Root Cause
- ...
```
