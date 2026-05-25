# 10 - Final Report

Produce final report in this exact format:

```md
# Final Report - Identity PostgreSQL DateTime UTC Fix

## 1. Problem Summary

- Exception:
- First observed handler:
- Failing save layer:

## 2. Root Cause

- DbContext:
- Entity:
- Property:
- Assigned from:
- DateTime.Kind:

## 3. IdentityDbContext Mapping

- Relevant column:
- PostgreSQL type:
- Why UTC is required:

## 4. Changed Files

- `path/to/file.cs`
  - ...

## 5. UTC Strategy Applied

- ...

## 6. DateTime Sources Fixed

| Location | Before | After |
|---|---|---|

## 7. ChangePassword Flow Impact

- ...

## 8. UnitOfWork / SaveChanges Impact

- ...

## 9. Verification

### Build
- ...

### Tests
- ...

### Manual API Result
- ...

## 10. What Not To Do

- Do not use `Npgsql.EnableLegacyTimestampBehavior` as final solution.
- Do not persist `DateTime.Now` into `timestamp with time zone`.
- Do not ignore inner exception details.

## 11. Follow-up Recommendations

- ...
```
