# Service Request — Mock Seed Data Prompt

**Target project:** `Aizen.Modules.ServiceRequest`
**Scope:** Generate comprehensive mock JSON files under
`Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/`
and wire them into `ServiceRequestMockDataSeeder.cs`.

**Environment:** Development and local only — seeder must be guarded so it never runs in staging/production.

---

## ⚠️ STEP 0 — Read existing structure first (MANDATORY)

### 0a. Read the existing JSON seed files

List and read every file under:
```
Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/Json/MockData/admin-demo/
```

For each file you find:
- Note the file name and the entity it seeds
- Note every field present (do NOT invent fields that don't exist in the entity)
- Note ID ranges and naming patterns (e.g. IDs starting at 9001, slugs like `sr-0001`)
- Note any foreign key values used (userId, providerId, vesselId, etc.)

### 0b. Read the existing Seeder

Read:
```
Modules/ServiceRequest/src/Aizen.Modules.ServiceRequest.Repository/Seed/MockData/ServiceRequestMockDataSeeder.cs
```

Understand:
- How it reads JSON files (e.g. `File.ReadAllText`, `JsonSerializer.Deserialize`, `EmbeddedResource`)
- How it guards against duplicate seeds (e.g. `if (!context.ServiceRequests.Any())`)
- How it resolves the JSON file path
- Whether it uses `DbContext` directly or a repository
- Which entities it already seeds and in which order
- The `IsDevelopment` / environment guard pattern

### 0c. Read Identity module user seed data

Search for and read any of:
```
Modules/Identity/src/*/Seed/Json/MockData/admin-demo/users.json
Modules/Identity/src/*/Seed/Json/MockData/admin-demo/organizers.json
Modules/Identity/src/*/Seed/Json/MockData/admin-demo/participants.json
Modules/Identity/src/*/Seed/Json/MockData/admin-demo/venues.json
```

From these files, extract:
- User IDs that represent **boat owners** (participants) — note at least 3
- User IDs that represent **providers** (organizers) — note at least 3
- User IDs that represent **venue/marina operators** — note at least 2
- Their names and email addresses (for denormalized fields in service requests)

### 0d. Read Vessel module seed data (if it exists)

Search for:
```
Modules/Vessel/src/*/Seed/Json/MockData/admin-demo/vessels.json
```

Extract at least 3 vessel IDs and their names (for `VesselId`, `VesselName` denormalized fields).
If the file does not exist, use placeholder IDs: 8001, 8002, 8003 with names: "M/Y Serenity", "S/V Aurora", "M/Y Poseidon".

Only after completing all Step 0 reads, proceed.

---

## Step 1 — service-requests.json

Create file:
```
.../admin-demo/service-requests.json
```

Generate **10 service request records** covering all statuses and urgency levels.

Schema (use only fields that exist on `ServiceRequestEntity` — confirmed in Step 0a):

```json
[
  {
    "id": 9001,
    "title": "Propulsion System Overhaul — Port Engine",
    "description": "Complete teardown and rebuild of the port-side diesel propulsion system. Owner reports excessive vibration above 2,200 RPM and irregular fuel consumption. Suspected crankshaft bearing wear.",
    "category": "Propulsion",
    "status": "in_progress",
    "priority": "urgent",
    "vesselId": <vessel_id_from_step_0d>,
    "vesselName": "M/Y Serenity",
    "requestedById": <owner_user_id_from_step_0c>,
    "requestedByEmail": <owner_email_from_step_0c>,
    "assignedToId": <provider_user_id_from_step_0c>,
    "assignedProviderName": <provider_name_from_step_0c>,
    "location": "Marina di Portofino — Berth 14A",
    "scheduledAt": "2026-07-01T09:00:00Z",
    "completedAt": null,
    "disputedAt": null,
    "disputeReason": null,
    "createdAt": "2026-06-10T08:30:00Z",
    "updatedAt": "2026-06-20T14:15:00Z"
  }
]
```

**Required distribution across 10 records:**

| # | Status | Priority | Category |
|---|--------|----------|----------|
| 9001 | in_progress | urgent | Propulsion |
| 9002 | pending | high | Electrical |
| 9003 | assigned | high | Navigation |
| 9004 | in_progress | medium | Hull & Deck |
| 9005 | completed | medium | Rigging |
| 9006 | disputed | urgent | Propulsion |
| 9007 | pending | low | Interior |
| 9008 | completed | medium | Safety Equipment |
| 9009 | cancelled | low | Plumbing |
| 9010 | assigned | high | Mechanical |

Use the **real user/vessel IDs** extracted in Step 0c and 0d.
Spread requests across the 3 boat owners and 3 providers you found.
All `createdAt` / `updatedAt` / `scheduledAt` values must be UTC ISO 8601 strings.
`disputedAt` and `disputeReason` must only be non-null on record 9006.
`completedAt` must only be non-null on records 9005 and 9008.

---

## Step 2 — provider-offers.json

Create file:
```
.../admin-demo/provider-offers.json
```

Generate **16 offer records** — multiple offers per service request for the in_progress, assigned, and disputed ones.

```json
[
  {
    "id": 9101,
    "serviceRequestId": 9001,
    "providerId": <provider_user_id>,
    "providerName": <provider_name>,
    "rating": 4.8,
    "reviewCount": 127,
    "quoteAmount": 82450.00,
    "currency": "USD",
    "estimatedDuration": "18 days",
    "proposalFileId": null,
    "status": "Accepted",
    "submittedAt": "2026-06-11T10:00:00Z"
  }
]
```

**Distribution rules:**

- Service request 9001 (in_progress/urgent): 3 offers — 1 Accepted, 2 Rejected
- Service request 9002 (pending): 2 offers — both Pending
- Service request 9003 (assigned): 2 offers — 1 Accepted, 1 Rejected
- Service request 9004 (in_progress): 2 offers — 1 Accepted, 1 Rejected
- Service request 9006 (disputed): 3 offers — 1 Accepted, 2 Rejected
- Service request 9010 (assigned): 2 offers — 1 Accepted, 1 Pending
- Service requests 9005, 9007, 9008, 9009: 0 offers (skip)

`status` values: `"Accepted"` | `"Rejected"` | `"Pending"` | `"Expired"` (PascalCase — enforced)
`rating` range: 3.5–5.0
`reviewCount` range: 10–200
`quoteAmount` range: 1,500–120,000 (realistic marine service pricing)

---

## Step 3 — work-log-entries.json

Create file:
```
.../admin-demo/work-log-entries.json
```

Generate **20 work log entries** for service requests 9001, 9004, and 9006 (the active/disputed ones).

```json
[
  {
    "id": 9201,
    "serviceRequestId": 9001,
    "type": "note",
    "content": "Initial diagnostic complete. Crankshaft bearing on cylinders 3 and 4 showing 0.08mm wear beyond tolerance. Ordered OEM replacement parts from MAN Diesel — ETA 3 days.",
    "mediaFileId": null,
    "author": <provider_name>,
    "authorId": <provider_user_id>,
    "timestamp": "2026-06-12T09:15:00Z"
  }
]
```

**Required distribution:**

Service request 9001 — 10 entries:
- 5 `type: "note"` — progress updates from lead technician
- 3 `type: "photo"` — with realistic `mediaFileId` values (e.g. `"file-sr9001-photo-001"`)
- 2 `type: "status_change"` — e.g. "Phase changed: Disassembly → Replacement"

Service request 9004 — 6 entries:
- 4 `type: "note"`
- 2 `type: "photo"`

Service request 9006 — 4 entries:
- 3 `type: "note"` — include one noting the dispute filing
- 1 `type: "status_change"`

`timestamp` values must be chronologically ordered within each service request.
`content` must be realistic marine maintenance English — not lorem ipsum.

---

## Step 4 — work-phases.json

Create file:
```
.../admin-demo/work-phases.json
```

Generate **5 phases for service request 9001** and **5 phases for service request 9004**.

```json
[
  {
    "id": 9301,
    "serviceRequestId": 9001,
    "phaseNumber": 1,
    "title": "Diagnostics & Assessment",
    "progressPercent": 100,
    "status": "Completed",
    "displayOrder": 1
  }
]
```

**Phases for 9001 (in_progress/urgent):**

| phaseNumber | title | progressPercent | status |
|-------------|-------|-----------------|--------|
| 1 | Diagnostics & Assessment | 100 | Completed |
| 2 | Disassembly & Inspection | 100 | Completed |
| 3 | Parts Replacement | 65 | In Progress |
| 4 | System Testing | 0 | Upcoming |
| 5 | Final Handover & Sign-off | 0 | Upcoming |

**Phases for 9004 (in_progress/hull):**

| phaseNumber | title | progressPercent | status |
|-------------|-------|-----------------|--------|
| 1 | Hull Inspection | 100 | Completed |
| 2 | Blasting & Surface Prep | 100 | Completed |
| 3 | Anti-fouling Application | 30 | In Progress |
| 4 | Topcoat & Finishing | 0 | Upcoming |
| 5 | Quality Inspection | 0 | Upcoming |

`status` values must be Title Case with space: `"Completed"` | `"In Progress"` | `"Upcoming"`

---

## Step 5 — sr-conversations.json

Create file:
```
.../admin-demo/sr-conversations.json
```

Generate **5 conversation records** — one per active service request.

```json
[
  {
    "id": 9401,
    "serviceRequestId": 9001,
    "title": "M/Y Serenity — Propulsion Overhaul",
    "status": "active",
    "unreadCount": 3,
    "lastMessageAt": "2026-06-23T16:45:00Z"
  },
  {
    "id": 9402,
    "serviceRequestId": 9002,
    "title": "Electrical System Fault — S/V Aurora",
    "status": "pending",
    "unreadCount": 0,
    "lastMessageAt": "2026-06-22T11:00:00Z"
  },
  {
    "id": 9403,
    "serviceRequestId": 9006,
    "title": "Disputed — Propulsion Repair M/Y Poseidon",
    "status": "flagged",
    "unreadCount": 5,
    "lastMessageAt": "2026-06-23T18:00:00Z"
  },
  {
    "id": 9404,
    "serviceRequestId": 9003,
    "title": "Nav System Integration — M/Y Serenity",
    "status": "active",
    "unreadCount": 1,
    "lastMessageAt": "2026-06-21T09:30:00Z"
  },
  {
    "id": 9405,
    "serviceRequestId": 9004,
    "title": "Hull Maintenance Progress — S/V Aurora",
    "status": "active",
    "unreadCount": 2,
    "lastMessageAt": "2026-06-23T14:20:00Z"
  }
]
```

`status` values: `"active"` | `"pending"` | `"flagged"` (lowercase)

---

## Step 6 — conversation-messages.json

Create file:
```
.../admin-demo/conversation-messages.json
```

Generate **25 message records** across the 5 conversations above.

Each conversation must have:
- At least 1 message from `"Owner"` senderRole
- At least 1 message from `"Provider"` senderRole
- At least 1 message from `"Admin"` senderRole with `"isInternalNote": true`

Conversation 9403 (flagged/disputed) must include:
- A message from Owner disputing the extra hours
- A message from Provider defending the overage
- An Admin internal note flagging for senior review

```json
[
  {
    "id": 9501,
    "conversationId": 9401,
    "senderId": <owner_user_id_as_string>,
    "senderName": <owner_name>,
    "senderRole": "Owner",
    "content": "Just wanted to confirm — has the replacement bearing arrived? The vessel needs to be back in service by July 15th for our Amalfi charter.",
    "isInternalNote": false,
    "sentAt": "2026-06-22T09:00:00Z"
  }
]
```

**Rules:**
- `senderRole` must be PascalCase: `"Owner"` | `"Provider"` | `"Admin"`
- `isInternalNote` must be `true` only for Admin messages marked as internal
- `senderId` must be a string (not integer)
- `sentAt` values must be chronologically ordered within each conversation
- `content` must be realistic English — no placeholder text
- Admin internal notes should sound like admin audit/review language (e.g. "Flagging this for senior compliance review. Budget variance exceeds 10% threshold per platform policy.")

---

## Step 7 — Update `ServiceRequestMockDataSeeder.cs`

Read the existing seeder (Step 0b) and extend it to seed all new entities.

**Exact requirements:**

1. **Environment guard** — follow the existing pattern. If none exists, add:
   ```csharp
   if (!environment.IsDevelopment()) return;
   ```

2. **Idempotency guard per entity** — before seeding each entity, check `Any()` on the primary key range:
   ```csharp
   if (context.ServiceRequests.Any(sr => sr.Id >= 9001 && sr.Id <= 9010)) return;
   ```
   Add similar guards for each of the 6 new tables.

3. **Seeding order** (respects foreign key dependencies):
   ```
   ServiceRequests (9001–9010)
   → ProviderOffers (depends on ServiceRequests)
   → WorkLogEntries (depends on ServiceRequests)
   → WorkPhases (depends on ServiceRequests)
   → SrConversations (depends on ServiceRequests)
   → ConversationMessages (depends on SrConversations)
   ```

4. **JSON loading** — follow the exact file-reading pattern from Step 0b (do not introduce a new pattern).

5. **Entity construction** — use the entity's constructor or factory method if one exists. Do NOT set private-set properties directly via object initializer unless the existing seeder already does this for other entities.

6. **SaveChanges** — call `await context.SaveChangesAsync()` after each entity batch (not once at the end), so FK violations are caught early.

7. **Logging** — add `ILogger<ServiceRequestMockDataSeeder>` logging at `Information` level:
   ```
   "Seeding {count} service requests..."
   "Seeded service requests successfully."
   ```
   Follow the log format used in the existing seeder.

---

## Step 8 — Smoke Validation

After the seeder runs, verify via EF queries (or a quick SQL check):

```sql
SELECT COUNT(*) FROM service_requests WHERE id BETWEEN 9001 AND 9010;
-- Expected: 10

SELECT COUNT(*) FROM provider_offers WHERE id BETWEEN 9101 AND 9116;
-- Expected: 16 (or however many you created)

SELECT COUNT(*) FROM work_log_entries WHERE service_request_id IN (9001, 9004, 9006);
-- Expected: 20

SELECT COUNT(*) FROM work_phases WHERE service_request_id IN (9001, 9004);
-- Expected: 10

SELECT COUNT(*) FROM sr_conversations WHERE id BETWEEN 9401 AND 9405;
-- Expected: 5

SELECT COUNT(*) FROM conversation_messages WHERE id BETWEEN 9501 AND 9525;
-- Expected: 25
```

Use the **actual table names** found in Step 0b — do not assume snake_case if the project uses PascalCase table names.

---

## Step 9 — Final Checklist

- [ ] All Step 0 reads completed before any file was created
- [ ] JSON field names match the actual entity properties (case-sensitive)
- [ ] All IDs are within the 9001+ range — no conflicts with existing seed data
- [ ] Foreign key IDs reference real records from Identity/Vessel seed data
- [ ] `offer.status` is PascalCase
- [ ] `workPhase.status` is Title Case with space ("In Progress", not "in_progress")
- [ ] `senderRole` is PascalCase
- [ ] `serviceRequest.status` is snake_case with underscore ("in_progress", not "InProgress")
- [ ] Seeder guards against re-seeding on duplicate runs
- [ ] Seeder does NOT run outside of Development environment
- [ ] `dotnet run` in Development seeds data without errors
- [ ] SQL smoke checks pass
