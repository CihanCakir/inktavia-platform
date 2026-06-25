# AdminPanel BFF — Inactive Endpoint Strategy Report

**Date:** 2026-06-15  
**Branch:** feature/service-request-registration  
**Scope:** `AdminInactiveModulesController` endpoint classification

---

## Summary

This report classifies every endpoint registered in `AdminInactiveModulesController` by its
recommended strategy: **501 Not Implemented**, **demo mock data**, or **real implementation**.

---

## Controller Configuration

```
File: Aizen.Bff.AdminPanel/Controllers/V1/AdminInactiveModulesController.cs
Access: [AllowAnonymous]  (intentional — 501 is a content-type response, not a secure resource)
```

---

## Endpoint Classification

### CargoDry

| Endpoint | Route | Strategy | Rationale |
|---|---|---|---|
| Get kits | `GET /cargodry/kits` | **501 — keep** | CargoDry module does not exist. No Admin Web screen depends on it for the current demo. |
| Activate kit | `POST /cargodry/kits/activate` | **501 — keep** | Same. |

### Notification Templates

| Endpoint | Route | Strategy | Rationale |
|---|---|---|---|
| List templates | `GET /notification-templates` | **501 — keep** | Notification template management module is not active. Admin Web settings screen is feature-flagged off. |
| Create template | `POST /notification-templates` | **501 — keep** | Same. |

### Payment

| Endpoint | Route | Strategy | Rationale |
|---|---|---|---|
| Transactions | `GET /payments/transactions` | **501 — keep** | Payment module is explicitly listed as inactive/future per architecture rules. |
| Transaction KPI | `GET /payments/transactions/kpi` | **501 — keep** | Same. |
| Commissions | `GET /payments/commissions` | **501 — keep** | Same. |

### Reporting / Analytics

| Endpoint | Route | Strategy | Rationale |
|---|---|---|---|
| Reports list | `GET /reports` | **501 — keep** | No Reporting module contract exists. Frontend reporting page is not part of the current Admin Web demo build. |
| Reports KPI | `GET /reports/kpi` | **501 — keep** | Same. |
| Analytics dashboard | `GET /analytics/dashboard` | **501 — keep** | Analytics data requires active Reporting module. Not present in current release. |

### File Storage — Listing

| Endpoint | Route | Strategy | Rationale |
|---|---|---|---|
| Files list | `GET /files` | **501 — keep** | FileStorage module exposes individual file metadata by ID. A general admin files listing endpoint is not part of the active FileStorage contract. The `/files/:id/read-url` endpoint is handled by the active `AdminFilesController`. |

---

## Summary Table

| Domain | Endpoint count | Strategy | Reason |
|---|---|---|---|
| CargoDry | 2 | 501 | Module does not exist |
| Notification | 2 | 501 | Module inactive, screen feature-flagged |
| Payment | 3 | 501 | Explicitly inactive per architecture |
| Reporting | 2 | 501 | No module contract |
| Analytics | 1 | 501 | Depends on inactive Reporting module |
| Files (listing) | 1 | 501 | Not part of active FileStorage contract |
| **Total** | **11** | **All 501** | |

---

## Decision: No Demo Mocks Needed

None of the 11 endpoints correspond to Admin Web screens that are expected to be **actively
testable in the current demo**. The active Admin Web demo relies on:

- Identity (login, profiles, vessels)
- ReferenceData (lookup groups, lookup items)
- Vessel (overview, documents, media)
- ServiceRequest (list, details, assignment)
- FileStorage (read URL, upload URL)

All of these are covered by the active BFF controllers. The 501 endpoints are for modules that are
explicitly deferred to a future release.

---

## Auth Consideration

`AdminInactiveModulesController` is decorated with `[AllowAnonymous]`. This is intentional:
the 501 responses do not expose any data, and returning 401 before 501 would mask the
"module not implemented" signal. If future policy requires auth before 501, add `[Authorize]`.

---

## Remaining Gaps

1. If the Admin Web frontend renders any of these routes and expects non-501 responses for a
   demo build, the strategy should be revisited to provide local mock data (stable JSON fixtures).
   At time of writing, no such requirement exists.

2. When any of these modules becomes active, the corresponding 501 controller actions should be
   removed and replaced by real BFF handlers in the appropriate domain controller.
