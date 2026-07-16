# Backend run order

`09_BACKEND_CLAUDE_CODE_PROMPT.md` is the **umbrella** — the whole backend scope in one document. **Do not run
it as one job.** It spans three phases, and a single agent run across three phases is exactly how we get reports
that say "complete" over code that never worked: the agent finishes the last thing it touched and summarises the
rest.

Run these **in order**, one at a time, verifying between them:

| # | Prompt | Scope | Gate before running |
|---|---|---|---|
| 0 | `00_PHASE0_PRODUCT_DECISIONS_AND_PREREQUISITES.md` | **Decisions, no code** | — |
| 1 | `09a_BACKEND_PHASE1_DATA_FOUNDATION.md` | `PublishedAt` · vessel snapshot · offer-state projection · cursor paging · indexes | **Budget decision must be answered** (it owns the migration) |
| 2 | `09b_BACKEND_PHASE2_GEO_AND_PRIVACY.md` | bbox + haversine in SQL · distance sort · coordinate snapping · city fallback · `LocationMode` | 09a merged and verified |
| 3 | `09c_BACKEND_PHASE3_BFF_REALTIME_AND_AUTH_TESTS.md` | 3 BFF endpoints · summary cache · realtime events · **auth/impersonation tests** | 09b merged and verified |

Then the frontend: `10_FRONTEND_CLAUDE_CODE_PROMPT.md` (Phases 4–6).

## Between each run — verify, do not trust the summary

Every report this project has produced overstated completion at least once: a consumer with no publisher, a
delete that could never purge, a "size check" that only logged, a backplane declared working that had never
carried a message across two processes, a city fix that left the system broken in a new way. The cost of
checking is minutes; the cost of not checking has been days.

After 09a: does a provider who already bid see the request, badged? Read the generated SQL — is any child
collection still being `Include`d?
After 09b: paste the SQL. Is the bbox using the index? Is any distance computed outside the database?
After 09c: send a `providerProfileId` from the client and prove it changes nothing.

## Parallel track (Identity / onboarding)

Service area + provider categories (doc 00 §1, §4) are independent of 09a–09c and can run alongside — but they
must land **before the map phase**, so the KPI can move from "in my city" to "in my service area" without a
second frontend pass.
