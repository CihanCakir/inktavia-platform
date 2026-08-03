# FIX — admin conversation list returns empty (BFF service token → total:0), diagnostic-first

> **Repo:** `addesso-project` (Messaging module + how it's reached from the AdminPanel BFF). Follow-up to admin messaging
> Wave 1: the realtime frame→refetch mechanism works, but the conversation **list is empty**, so there is nothing to
> observe. This blocks the actual feature.

## Proven by static analysis (do NOT "fix" these — they're already correct)
- The module list query is **unscoped**: `GetConversationListQueryHandler` → `ConversationRepository.GetListAsync` /
  `CountAsync` filter only on `!IsDeleted` (+ optional status/contextType) — **no caller / userId / tenant filter**. So
  the query is already admin-correct: it returns *all* conversations.
- There is **no EF global query filter** and **no ICurrentUser scoping** in the Messaging module.
- `MessagingMockDataSeeder` **does** seed conversations (SR 9001, 9004, …) with participants (incl. an `Admin` userId=1)
  and messages; it's idempotent (`if (await _db.Conversations.AnyAsync(ct)) return;`).
- The AdminPanel BFF forwards `status/contextType/skip/take` transparently and injects no filter.

Conclusion: the empty result is **not** query logic. The same unscoped query returns rows for a user token but `total:0`
for the BFF service token → the two identities are reaching **different data** (different database, or the seed landed in
a different DB than the module reads at runtime). This is the same class as the earlier `aizen` ↔ `inktavia_store` split
(some seeds historically landed in the legacy `aizen` DB while runtime reads `inktavia_store`).

## Strong lead
**Other admin BFF→module reads (finance, payment, provider/participant plans) return seeded data through the same admin
service-token mechanism.** Messaging is the outlier. So the divergence is messaging-specific: its DB connection, the DB
its seeder wrote to, or its DB name. **Compare messaging's DB wiring to a known-working module's** (e.g. Payment) to
pinpoint it.

## Diagnose (runtime — Claude Code has it)
1. Identify the database the **Messaging module uses at runtime as reached by the AdminPanel BFF** (the resolved
   connection string / DB name). Compare to the DB a **working** admin module (Payment) uses. Are they the same DB
   instance/name? (Prime suspect: messaging reads/writes `aizen` while the working modules use `inktavia_store`, or vice
   versa.)
2. Query that DB directly: `SELECT count(*) FROM <messaging conversations table> WHERE is_deleted = false;` — is it 0?
   And in the *other* DB (`aizen` vs `inktavia_store`), is it non-zero? That reveals whether the seed landed in a DB the
   runtime module doesn't read.
3. Confirm whether the "rows for a user token" observation hit a **different** module instance/DB than the admin BFF's
   service-token path (e.g. provider-web/provider-BFF path vs admin-BFF path). If a **token-keyed connection/tenant
   resolver** exists in the shared Core layer (`AizenClientInfoMiddleware` / `IAizenClientInfoAccessor` / the
   Core.Persistence connection resolution), determine whether the service token resolves to a different DB than a user
   token — that would be the exact cause.

## Fix (per the finding — pick the one the diagnosis proves)
- **If the seed/data is in the wrong DB** (aizen vs inktavia_store): make the Messaging module read/write the **same**
  runtime DB the other modules use (align its connection to `inktavia_store` or wherever Payment reads), and (re)run
  `MessagingMockDataSeeder` against that DB — mirror exactly how a working module's connection + seed is wired. (This is
  the same remedy applied to the I1 sub-merchant seed earlier.)
- **If a token-keyed connection/tenant resolver** routes the admin service token to an empty/different DB: align the
  admin BFF→messaging path to resolve the same DB as the working admin reads (match the finance/payment wiring), so the
  service token reaches the real conversations.
- Do **not** add caller-scoping to the query (it's correct as-is); do not weaken auth.

## Verify (on-screen)
1. `/app/messages` lists the seeded conversations (SR 9001 / 9004 …) with the ALL / PENDING / FLAGGED tabs, unread
   badges, previews — for the admin (service-token) path, no user token needed.
2. Selecting a conversation loads its messages; the Wave-1 live frame still triggers a refetch that now shows content.
3. A working admin module (Payment) still returns its data (no regression from any connection change). Backend builds
   clean.

## Report
`docs/V1.0.1/Messaging/REPORT_FIX_ADMIN_CONVERSATION_LIST_EMPTY.md`: the DB the messaging module actually used vs the
working modules, the row counts per DB, the exact cause (seed-in-wrong-DB vs token-keyed routing), the fix applied, and
the on-screen populated list. Confirm the query was left unscoped (unchanged).
