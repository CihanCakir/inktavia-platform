# 09a.1 — Phase 1 is not finished: the migration and the vessel producer

Follow-up to `REPORT_BACKEND_09a.md`. The report's summary says "all tasks complete". **Two of them are not**,
and each one alone makes the phase non-functional. Do not start 09b until these are closed.

Everything else in 09a checks out: the `Select` projection with `.Count()` / `.Any()` subqueries, the caller's
own offer projected without materialising foreign offer rows, cursor pagination with a filter fingerprint, the
`OfferState` predicate replacing the hard exclusion, identity rejected rather than defaulted, no
`ProviderProfileId` on the filter, no budget. That work is good. These two gaps are what remains.

---

## 1. There is no migration. The columns exist only in C#.

The newest migration in `Aizen.Modules.ServiceRequest.Repository/Migrations/` is **`20260707143031_AddClientRatingToServiceRequestCompletions`**.
`PublishedAt` and the five vessel columns were added to `ServiceRequestEntity` and to the EF configuration — and
nowhere else.

So the database has no such columns. The first discovery query will fail with `column "PublishedAt" does not
exist`. The report acknowledges this in passing ("backfill SQL ready but needs the EF migration first") while
the summary reports the task as done. **A column that exists in the entity and not in the database is not a
column; it is a compile-time illusion.**

Do:

- Create the EF migration (`PublishedAt`, `VesselTypeCode`, `VesselManufacturer`, `VesselModel`,
  `VesselLengthValue`, `VesselLengthUnitCode`) plus the indexes 09a specified:
  `(Status, PublishedAt)`, `(Status, LocationCityCode)`, and the title search index.
- Apply it and **run the backfill**: `PublishedAt = CreateDate` for rows already in a published status; never-
  published rows stay `null`. Report that these values are **approximate** — `CreateDate` is draft creation.
- **Prove it against the running database**, not against the code: paste `\d "ServiceRequests"` (or the
  equivalent) showing the columns and the indexes, and a discovery query returning rows.

## 2. The vessel snapshot has no producer. Five columns that are always null.

`ServiceRequestEntity.Publish(...)` takes five vessel parameters. Its only caller passes none:

```csharp
// PublishServiceRequestCommandHandler.cs:54
entity.Publish();
```

With a comment saying the columns "are set here so they are ready when the Vessel integration is added". They are
not set. They will be `null` for every request ever published.

**This is precisely the pattern we refused for budget**: a column with no producer. It renders as emptiness,
nobody can tell whether the data is missing or the feature is broken, and it will sit there for a year.

The reasoning that led here was a misreading of the N+1 rule. **The N+1 concern is on the read side** — one
Vessel call per *card*, on every keystroke and every map pan. **One Vessel call per request, once, at
publication** is not N+1; it is the entire point of a snapshot. That call was always in scope.

Do **one** of the following — not a third thing, and not nothing:

**(a) Build the producer (preferred, and what 09a asked for).**
- Add a Vessel remote call to ServiceRequest following the existing `AizenRemoteCall` / Refit conventions used
  for `IReferenceDataRemoteCall` in this very handler (it already calls ReferenceData to validate the city — the
  pattern is right there, in the same method).
- In `PublishServiceRequestCommandHandler`, fetch the vessel by `entity.VesselId` and pass
  `VesselTypeCode`, `VesselManufacturer`, `VesselModel`, `VesselLengthValue`, `VesselLengthUnitCode` into
  `Publish(...)`. One call, once, at publication.
- Failure policy — decide and state it: if Vessel is unreachable, does publication **fail** or proceed with a
  `null` snapshot? Recommendation: **proceed with null and log a warning.** A missing boat length must not stop
  an owner from publishing a job; the card simply omits the field. Do not silently retry forever, and do not
  fabricate values.
- Backfill existing published requests from Vessel where the vessel still exists; `null` where it does not.
  **Do not guess.**

**(b) Delete the columns.** If the Vessel call is not going to be built in this phase, remove the five columns,
the DTO fields and the `Publish(...)` parameters. A schema that promises data nobody writes is worse than an
honest absence — it teaches the next reader to trust something that is never true.

Choose (a) or (b) and say which. "Ready for when the integration ships" is neither.

---

## Acceptance — observed, not asserted

- [ ] The migration exists, is applied, and the columns + indexes are visible **in the database** (paste the
      schema output).
- [ ] A published request has a **non-null `PublishedAt`**; republishing it does not change the value.
- [ ] Either: a newly published request has a **populated vessel snapshot** (paste the row) — or the columns are
      gone entirely. Nothing in between.
- [ ] The discovery endpoint returns rows against the real database.
- [ ] A provider who has already bid sees the request, badged (verify with real data, not a unit test).

## Report

Append to `REPORT_BACKEND_09a.md` (do not start a new file): what the database actually shows, which vessel
option you took and why, and the failure policy for an unreachable Vessel module.

**And a process note, because this is the fourth time**: if a required step could not be completed, the phase is
**not complete** — say so in the summary line, not only in a paragraph in the middle. A summary that says "all
tasks complete" over a schema that does not exist is the most expensive sentence in this project.
