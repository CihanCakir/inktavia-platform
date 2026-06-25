# WorkLogs — Seed Data Prompt

**Target project:** `Aizen.Modules.ServiceRequest`
**Prerequisite:** `PROMPT_A_WORKLOGS_MICROSERVICE.md` complete — entities and migration applied.
**Scope:** Create JSON seed files for `work_log_entries` and `work_phases`, extend `ServiceRequestMockDataSeeder.cs` to seed them, and add SignalR simulation seeds for real-time testing.

---

## ⚠️ STEP 0 — MANDATORY reads before writing anything

### 0a. Read the existing seeder

```
Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/MockData/ServiceRequestMockDataSeeder.cs
```

Note:
- How JSON files are read (`File.ReadAllText`, `EmbeddedResource`, `Assembly.GetManifestResourceStream`?)
- Guard pattern against duplicates (e.g., `if (!context.ServiceRequests.Any())`)
- Order of seeding (service requests must exist before work-log entries / phases are seeded)
- Environment guard (`IsDevelopment`, `EnvironmentName == "Development"`, or similar)
- Error handling pattern (`try/catch`, `ILogger`, or silent)

### 0b. Read existing JSON seed files

```
ls Modules/ServiceRequest/src/.../Seed/Json/MockData/admin-demo/
```

Read each file and note:
- Which entities they seed
- ID range used (e.g., IDs starting at 9001)
- Foreign key values (user IDs, vessel IDs) — use these exact values in new seed files
- Date format used (UTC ISO 8601: `"2026-06-10T08:30:00Z"`)

### 0c. Confirm existing service request IDs

From the service-requests seed file, confirm which IDs exist with `status = "in_progress"` or `status = 30` (inprogress int code). You will only create work-log entries and phases for IDs that are in active statuses.

From previous definitions:
- `9001` — inprogress, urgent, Propulsion
- `9004` — inprogress, medium, Hull & Deck
- `9006` — disputeopened, urgent, Propulsion (disputed — partial phases)
- `9010` — assigned, high, Mechanical (phases exist but no log entries yet)

If actual IDs differ in your seed file, adjust accordingly.

Only after Step 0 is complete, proceed.

---

## Step 1 — `work-phases.json`

Create file at:
```
.../Seed/Json/MockData/admin-demo/work-phases.json
```

Each work phase record must include only fields that exist on `WorkPhaseEntity` (confirmed in Step 0b + PROMPT_A Step 1c).

### Schema

```json
[
  {
    "id": <integer>,
    "serviceRequestId": <integer>,
    "phaseNumber": <integer>,
    "title": <string, max 128 chars>,
    "progressPercent": <integer, 0-100>,
    "status": "Upcoming" | "In Progress" | "Completed",
    "displayOrder": <integer>
  }
]
```

### Content

Generate the following records. IDs start at `9501`.

**SR 9001 — Propulsion System Overhaul (in_progress, Phase 3 active)**

```json
[
  {
    "id": 9501,
    "serviceRequestId": 9001,
    "phaseNumber": 1,
    "title": "Diagnostics",
    "progressPercent": 100,
    "status": "Completed",
    "displayOrder": 1
  },
  {
    "id": 9502,
    "serviceRequestId": 9001,
    "phaseNumber": 2,
    "title": "Disassembly & Inspection",
    "progressPercent": 100,
    "status": "Completed",
    "displayOrder": 2
  },
  {
    "id": 9503,
    "serviceRequestId": 9001,
    "phaseNumber": 3,
    "title": "Component Replacement",
    "progressPercent": 60,
    "status": "In Progress",
    "displayOrder": 3
  },
  {
    "id": 9504,
    "serviceRequestId": 9001,
    "phaseNumber": 4,
    "title": "System Testing",
    "progressPercent": 0,
    "status": "Upcoming",
    "displayOrder": 4
  },
  {
    "id": 9505,
    "serviceRequestId": 9001,
    "phaseNumber": 5,
    "title": "Final Handover",
    "progressPercent": 0,
    "status": "Upcoming",
    "displayOrder": 5
  }
]
```

**SR 9004 — Hull & Deck Maintenance (in_progress, Phase 2 active)**

```json
[
  {
    "id": 9511,
    "serviceRequestId": 9004,
    "phaseNumber": 1,
    "title": "Surface Assessment",
    "progressPercent": 100,
    "status": "Completed",
    "displayOrder": 1
  },
  {
    "id": 9512,
    "serviceRequestId": 9004,
    "phaseNumber": 2,
    "title": "Hull Cleaning & Prep",
    "progressPercent": 45,
    "status": "In Progress",
    "displayOrder": 2
  },
  {
    "id": 9513,
    "serviceRequestId": 9004,
    "phaseNumber": 3,
    "title": "Anti-Fouling Coating",
    "progressPercent": 0,
    "status": "Upcoming",
    "displayOrder": 3
  },
  {
    "id": 9514,
    "serviceRequestId": 9004,
    "phaseNumber": 4,
    "title": "Quality Inspection",
    "progressPercent": 0,
    "status": "Upcoming",
    "displayOrder": 4
  }
]
```

**SR 9006 — Propulsion Dispute (disputeopened — phases show completed work before dispute)**

```json
[
  {
    "id": 9521,
    "serviceRequestId": 9006,
    "phaseNumber": 1,
    "title": "Diagnostics",
    "progressPercent": 100,
    "status": "Completed",
    "displayOrder": 1
  },
  {
    "id": 9522,
    "serviceRequestId": 9006,
    "phaseNumber": 2,
    "title": "Overhaul Work",
    "progressPercent": 100,
    "status": "Completed",
    "displayOrder": 2
  },
  {
    "id": 9523,
    "serviceRequestId": 9006,
    "phaseNumber": 3,
    "title": "Final Sign-Off",
    "progressPercent": 80,
    "status": "In Progress",
    "displayOrder": 3
  }
]
```

**SR 9010 — Assigned (phases defined but not started)**

```json
[
  {
    "id": 9531,
    "serviceRequestId": 9010,
    "phaseNumber": 1,
    "title": "Initial Assessment",
    "progressPercent": 0,
    "status": "Upcoming",
    "displayOrder": 1
  },
  {
    "id": 9532,
    "serviceRequestId": 9010,
    "phaseNumber": 2,
    "title": "Repair & Replacement",
    "progressPercent": 0,
    "status": "Upcoming",
    "displayOrder": 2
  },
  {
    "id": 9533,
    "serviceRequestId": 9010,
    "phaseNumber": 3,
    "title": "Final Testing",
    "progressPercent": 0,
    "status": "Upcoming",
    "displayOrder": 3
  }
]
```

Combine all records into a single `work-phases.json` array (total: 17 records).

---

## Step 2 — `work-log-entries.json`

Create file at:
```
.../Seed/Json/MockData/admin-demo/work-log-entries.json
```

Schema (use only fields that exist on `WorkLogEntryEntity`):

```json
[
  {
    "id": <integer>,
    "serviceRequestId": <integer>,
    "type": "note" | "photo" | "status_change",
    "content": <string, max 4000 chars>,
    "mediaFileId": <string | null>,
    "author": <string, max 256 chars>,
    "authorId": <integer | null>,
    "timestamp": <UTC ISO 8601 string>
  }
]
```

IDs start at `9601`. All timestamps are UTC. Entries are ordered newest-first per SR (the handler orders by `Timestamp DESC` already).

**SR 9001 — Active propulsion overhaul (8 entries)**

```json
[
  {
    "id": 9601,
    "serviceRequestId": 9001,
    "type": "note",
    "content": "Secondary fuel filter assembly successfully mounted. Verified torque settings on all primary mounting bolts at 85 Nm. Commencing sensor recalibration sequence.",
    "mediaFileId": null,
    "author": "Marcus Thorne",
    "authorId": null,
    "timestamp": "2026-06-24T14:32:00Z"
  },
  {
    "id": 9602,
    "serviceRequestId": 9001,
    "type": "photo",
    "content": "Post-installation verification photos — fuel filter assembly and primary mounting bracket.",
    "mediaFileId": "file-wl-001",
    "author": "Marcus Thorne",
    "authorId": null,
    "timestamp": "2026-06-24T13:55:00Z"
  },
  {
    "id": 9603,
    "serviceRequestId": 9001,
    "type": "status_change",
    "content": "Phase 3 (Component Replacement) progress updated to 60%. Custom gasket set MX-202 installed. High-pressure seals verified on-site.",
    "mediaFileId": null,
    "author": "Logistics Hub",
    "authorId": null,
    "timestamp": "2026-06-24T11:15:00Z"
  },
  {
    "id": 9604,
    "serviceRequestId": 9001,
    "type": "note",
    "content": "Daily brief completed. Focus today: completion of Phase 3. No deviations from budget reported. Safety audit cleared. Crew of 3 on-site.",
    "mediaFileId": null,
    "author": "Sarah Chen",
    "authorId": null,
    "timestamp": "2026-06-24T09:00:00Z"
  },
  {
    "id": 9605,
    "serviceRequestId": 9001,
    "type": "photo",
    "content": "Crankshaft bearing wear documentation — before replacement. Engineering sign-off required.",
    "mediaFileId": "file-wl-002",
    "author": "Marcus Thorne",
    "authorId": null,
    "timestamp": "2026-06-23T16:20:00Z"
  },
  {
    "id": 9606,
    "serviceRequestId": 9001,
    "type": "status_change",
    "content": "Phase 2 (Disassembly & Inspection) marked Completed. Engine block fully exposed. Bearing damage confirmed on #3 and #4 journals.",
    "mediaFileId": null,
    "author": "Marcus Thorne",
    "authorId": null,
    "timestamp": "2026-06-22T17:00:00Z"
  },
  {
    "id": 9607,
    "serviceRequestId": 9001,
    "type": "note",
    "content": "Parts delivery confirmed: custom gasket set (Item ID: MX-202) and high-pressure seals received and inspected. No defects. Cleared for installation.",
    "mediaFileId": null,
    "author": "Logistics Hub",
    "authorId": null,
    "timestamp": "2026-06-21T10:30:00Z"
  },
  {
    "id": 9608,
    "serviceRequestId": 9001,
    "type": "status_change",
    "content": "Phase 1 (Diagnostics) Completed. Root cause confirmed: crankshaft bearing wear on port engine journals #3 and #4. Fuel consumption anomaly traced to injector timing drift.",
    "mediaFileId": null,
    "author": "Marcus Thorne",
    "authorId": null,
    "timestamp": "2026-06-20T15:00:00Z"
  }
]
```

**SR 9004 — Hull & Deck maintenance (5 entries)**

```json
[
  {
    "id": 9611,
    "serviceRequestId": 9004,
    "type": "note",
    "content": "Underwater hull cleaning at 45% completion. Barnacle fouling heavier than expected below waterline aft section. Extending Phase 2 by approximately 6 hours.",
    "mediaFileId": null,
    "author": "D. Varga",
    "authorId": null,
    "timestamp": "2026-06-24T10:00:00Z"
  },
  {
    "id": 9612,
    "serviceRequestId": 9004,
    "type": "photo",
    "content": "Pre-cleaning hull survey — port side aft. Fouling level: heavy. Documentation for owner approval.",
    "mediaFileId": "file-wl-003",
    "author": "D. Varga",
    "authorId": null,
    "timestamp": "2026-06-23T14:30:00Z"
  },
  {
    "id": 9613,
    "serviceRequestId": 9004,
    "type": "status_change",
    "content": "Phase 1 (Surface Assessment) Completed. Hull condition report filed. Anti-fouling coating selection confirmed with owner: Hempel Olympic+.",
    "mediaFileId": null,
    "author": "D. Varga",
    "authorId": null,
    "timestamp": "2026-06-22T12:00:00Z"
  },
  {
    "id": 9614,
    "serviceRequestId": 9004,
    "type": "note",
    "content": "Marina dive team coordinated. Tidal window confirmed for 06:00–09:00 tomorrow. Equipment checklist signed off.",
    "mediaFileId": null,
    "author": "D. Varga",
    "authorId": null,
    "timestamp": "2026-06-21T16:00:00Z"
  },
  {
    "id": 9615,
    "serviceRequestId": 9004,
    "type": "status_change",
    "content": "Work order initiated. Vessel positioned at service berth. Owner briefed on scope. Phase 1 begin.",
    "mediaFileId": null,
    "author": "Coordination",
    "authorId": null,
    "timestamp": "2026-06-20T08:30:00Z"
  }
]
```

**SR 9006 — Dispute (3 entries — last entry triggered dispute)**

```json
[
  {
    "id": 9621,
    "serviceRequestId": 9006,
    "type": "status_change",
    "content": "DISPUTE FILED by owner. Reason: Labor hours (142.5h) exceed authorized quote of 120h without prior Change Order. Final payment withheld pending resolution.",
    "mediaFileId": null,
    "author": "System",
    "authorId": null,
    "timestamp": "2026-06-20T18:00:00Z"
  },
  {
    "id": 9622,
    "serviceRequestId": 9006,
    "type": "note",
    "content": "Technician final sign-off submitted. Manifest v2 uploaded. Total labor: 142.5 hours. All phases completed per technical scope.",
    "mediaFileId": "file-wl-004",
    "author": "Lead Tech",
    "authorId": null,
    "timestamp": "2026-06-19T17:00:00Z"
  },
  {
    "id": 9623,
    "serviceRequestId": 9006,
    "type": "status_change",
    "content": "Phase 2 (Overhaul Work) Completed. Full propulsion rebuild documented. 34 parts replaced, 142.5 man-hours logged.",
    "mediaFileId": null,
    "author": "Lead Tech",
    "authorId": null,
    "timestamp": "2026-06-18T15:30:00Z"
  }
]
```

Combine all records into a single `work-log-entries.json` array (total: 16 entries).

---

## Step 3 — Extend `ServiceRequestMockDataSeeder.cs`

Following the exact pattern found in Step 0a, add seeding for work phases and work-log entries.

### Seeding order (CRITICAL — foreign key dependencies)

```
1. ServiceRequests      (must exist first)
2. ProviderOffers       (FK → ServiceRequests)
3. WorkPhases           (FK → ServiceRequests)   ← NEW
4. WorkLogEntries       (FK → ServiceRequests)   ← NEW
```

### Code to add

```csharp
// ── Seed WorkPhases ──────────────────────────────────────────────────────────
// Guard: only seed if the table is empty
if (!await context.WorkPhases.AnyAsync(cancellationToken))
{
    var phasesJson = ReadJsonFile("work-phases.json");   // use the method already in Step 0a
    var phases = JsonSerializer.Deserialize<List<WorkPhaseEntity>>(
        phasesJson, _jsonOptions)          // use the JsonSerializerOptions already in seeder
        ?? [];

    await context.WorkPhases.AddRangeAsync(phases, cancellationToken);
    await context.SaveChangesAsync(cancellationToken);
    _logger.LogInformation("Seeded {Count} work phases.", phases.Count);
}

// ── Seed WorkLogEntries ──────────────────────────────────────────────────────
if (!await context.WorkLogEntries.AnyAsync(cancellationToken))
{
    var entriesJson = ReadJsonFile("work-log-entries.json");
    var entries = JsonSerializer.Deserialize<List<WorkLogEntryEntity>>(
        entriesJson, _jsonOptions)
        ?? [];

    await context.WorkLogEntries.AddRangeAsync(entries, cancellationToken);
    await context.SaveChangesAsync(cancellationToken);
    _logger.LogInformation("Seeded {Count} work log entries.", entries.Count);
}
```

> ⚠️ Replace `ReadJsonFile`, `_jsonOptions`, `_logger`, `context.WorkPhases`, `context.WorkLogEntries` with the actual field/method names found in Step 0a. Do NOT invent new helper methods — reuse what is already there.

---

## Step 4 — Verify seed data consistency

After seeding, run these checks:

```sql
-- Confirm work phases seeded
SELECT service_request_id, COUNT(*) as phase_count
FROM work_phases
GROUP BY service_request_id
ORDER BY service_request_id;
-- Expected: 9001→5, 9004→4, 9006→3, 9010→3

-- Confirm only one "In Progress" phase per SR
SELECT service_request_id, COUNT(*) as in_progress_count
FROM work_phases
WHERE status = 'In Progress'
GROUP BY service_request_id;
-- Each count should be 1 (or 0 for Upcoming-only SRs)

-- Confirm work log entries
SELECT service_request_id, COUNT(*) as entry_count, type, COUNT(*) as per_type
FROM work_log_entries
GROUP BY service_request_id, type
ORDER BY service_request_id, type;
-- Expected entries: 9001→8, 9004→5, 9006→3

-- Confirm GetWorkLogs query output for SR 9001
SELECT
    sr.id,
    sr.title,
    (SELECT COUNT(*) FROM work_phases WHERE service_request_id = sr.id) as phases,
    (SELECT COUNT(*) FROM work_log_entries WHERE service_request_id = sr.id) as log_entries
FROM service_requests sr
WHERE sr.id = 9001;
```

---

## Step 5 — BFF endpoint test with seed data

```bash
# After seeding, test the BFF endpoint:

GET /api/admin/service-requests/9001/work-logs

# Expected response shape:
{
  "header": { "isSuccess": true },
  "body": {
    "serviceRequestId": "9001",
    "title": "Propulsion System Overhaul — Port Engine",
    "currentPhase": {
      "number": 3,
      "title": "Component Replacement",
      "progressPercent": 60,
      "status": "In Progress"
    },
    "phases": [
      { "number": 1, "title": "Diagnostics",              "progressPercent": 100, "status": "Completed"  },
      { "number": 2, "title": "Disassembly & Inspection", "progressPercent": 100, "status": "Completed"  },
      { "number": 3, "title": "Component Replacement",    "progressPercent": 60,  "status": "In Progress" },
      { "number": 4, "title": "System Testing",           "progressPercent": 0,   "status": "Upcoming"   },
      { "number": 5, "title": "Final Handover",           "progressPercent": 0,   "status": "Upcoming"   }
    ],
    "logEntries": [
      { "id": "9601", "type": "note",         "author": "Marcus Thorne", "mediaUrl": null },
      { "id": "9602", "type": "photo",        "author": "Marcus Thorne", "mediaUrl": null },
      ...
    ],
    "providerActivity": {
      "providerName": "<provider name from SR 9001 assignment>",
      ...
    },
    "jobHealth": {
      "score": <integer>,
      "label": "Healthy" | "At Risk" | "Critical",
      "milestoneReached": "<string>"
    }
  }
}
```

Verify the React frontend's `WorkLogsPage` renders without crashing after the real BFF response is returned.

---

## Step 6 — Final Checklist

- [ ] Step 0 reads completed before any file written
- [ ] `work-phases.json` has 17 records covering SR 9001/9004/9006/9010
- [ ] `work-log-entries.json` has 16 records covering SR 9001/9004/9006
- [ ] Each SR has at most 1 phase with `status = "In Progress"`
- [ ] All timestamps are UTC ISO 8601 (`Z` suffix)
- [ ] All IDs are unique integers (no duplicates across entity types)
- [ ] `mediaFileId` is `null` for `note` and `status_change` entries (only `photo` entries have it)
- [ ] Seeder guards (`AnyAsync` check) prevent duplicate seeding on restart
- [ ] Seeding order respected: `service_requests` → `work_phases` → `work_log_entries`
- [ ] SQL verification queries run and match expected counts
- [ ] `GET /api/admin/service-requests/9001/work-logs` returns correct phases array
- [ ] React `WorkLogsPage` renders with real data (no crashes)
