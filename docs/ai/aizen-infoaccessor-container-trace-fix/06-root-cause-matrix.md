# 06 - Root Cause Matrix

Use evidence to select the exact cause.

## Possible Causes

### A - Different Container Instance
Evidence: middleware container hash differs from accessor/handler container hash.

### B - Different DI Scope
Evidence: handler is resolved in a new scope, so scoped container state is lost.

### C - Type Mismatch
Evidence: middleware `AizenUserInfo` type/assembly differs from accessor `AizenUserInfo` type/assembly.

### D - Accessor Reads Different Source
Evidence: middleware writes container, accessor reads field/other container/HttpContext.Items, or reverse.

### E - Middleware Timing
Evidence: `container.Set(userInfo)` occurs after `_next(context)`.

### F - Middleware Not in Actual Pipeline
Evidence: middleware does not run for the failing endpoint.

### G - Container Implementation Bug
Evidence: immediate `Get` after `Set` fails in the same middleware.

## Output Required

```md
# Root Cause Matrix Result

## Confirmed Cause
- ...

## Evidence
- ...

## Rejected Causes
- ...

## Fix Direction
- ...
```
