# ServiceRequest Demo Seed Implementation Report

**Date**: 2026-06-19  
**Branch**: feature/service-request-registration  
**Scope**: ServiceRequest demo seed data for Vessel Detail service history integration

---

## 1. Overview

The ServiceRequest mock seed data was already fully implemented in a prior session. This report documents the current state and its integration with the Vessel demo seed data.

---

## 2. Pre-existing ServiceRequest Seed Files

**Seeder**: `Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/MockData/ServiceRequestMockDataSeeder.cs`

**JSON Files** (`Seed/Json/MockData/admin-demo/`):
- `service-requests.json` — 12 service request records (IDs 30001–30012)
- `service-request-items.json`
- `service-request-offers.json`
- `service-request-offer-items.json`
- `service-request-assignments.json`
- `service-request-worklogs.json`
- `service-request-completions.json`
- `service-request-disputes.json`
- `service-request-messages.json`
- `service-request-status-history.json`

---

## 3. Vessel-to-ServiceRequest Linkage

ServiceRequest records link to seeded vessels:

| ServiceRequest ID | RequestCode | VesselId | Vessel Name | Status |
|------------------|------------|----------|-------------|--------|
| 30001 | SR-MOCK-2024-0001 | 20001 | Blue Octopus | Completed (41) |
| 30002 | SR-MOCK-2024-0002 | 20003 | Marmara Pearl | InProgress (30) |
| 30003 | SR-MOCK-2024-0003 | 20002 | Golden Tide | Completed (40) |
| 30004 | SR-MOCK-2024-0004 | 20007 | Bodrum Star | Pending (21) |
| 30005 | SR-MOCK-2025-0005 | 20005 | CargoDry Demo | Open (11) |
| 30006 | SR-MOCK-2025-0006 | 20006 | Kalamış Runner | Disputed (50) |
| 30007 | SR-MOCK-2024-0007 | 20004 | Aegean Wind | Completed (41) |
| 30008 | SR-MOCK-2025-0008 | 20008 | Göcek Breeze | Draft (1) |
| 30009 | SR-MOCK-2025-0009 | 20001 | Blue Octopus | Open (12) |
| 30010 | SR-MOCK-2025-0010 | 20003 | Marmara Pearl | Cancelled (90) |
| 30011 | SR-MOCK-2025-0011 | 20007 | Bodrum Star | Pending (22) |
| 30012 | SR-MOCK-2025-0012 | 20005 | CargoDry Demo | InProgress (32) |

---

## 4. ID Types Confirmed (ServiceRequest Module)

| Property | Type |
|----------|------|
| ServiceRequestEntity.Id | `long` |
| ServiceRequestEntity.VesselId | `long` |
| ServiceRequestEntity.OwnerUserId | `long` |
| OwnerProfileId | `long?` |
| ProviderProfileId | `long?` |

No Guid IDs used in ServiceRequest module.

---

## 5. Seeder Registration

Enabled via `appsettings.Local.json`:
```json
"MockData": {
  "Enabled": true,
  "RunOnStartup": true,
  "EnvironmentGuard": ["Local", "Development"],
  "DataSet": "admin-demo"
}
```

**Important**: Run Vessel seed FIRST to ensure vessel IDs 20001–20008 exist before ServiceRequest seed runs. Both seeders run at their respective API startup — no manual ordering is required if Vessel API starts before ServiceRequest API, or if re-run is acceptable.

---

## 6. Duplicate Guard

ServiceRequest seeder guards on `RequestCode`:
```csharp
if (await _db.ServiceRequests.AnyAsync(r => r.RequestCode == model.RequestCode, ct))
    continue;
```

---

## 7. Remaining Gaps

| Gap | Severity |
|-----|----------|
| ServiceRequest seed depends on Vessel seed completing first (cross-service dependency) | Low — acceptable for local dev |
| No status enum mapping table exists in ServiceRequest abstraction docs | Low |
