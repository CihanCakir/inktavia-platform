# Seed (Dev/Local only): reset job 91001 to Assigned + clean test data

Two goals: (1) clear the **test work logs** and lifecycle state accumulated while verifying JD-1..JD-5 so the İş Kayıtları
feed is clean, and (2) put assignment **91001** / SR **9011** back to **Assigned** so we can re-run the Start flow live and
watch the corrected RT toast ("İş güncellemesi") fire. Idempotent, guarded, Dev/Local only — do nothing in other envs.

## Verified in source (2026-07-17)
- Assignment `91001` (provider2) / SR `9011`. Current state: `CompletionSubmitted` with several work logs + a completion
  record + `JOB_STARTED` / completion system messages (all created during verification).
- Entities involved: `ServiceRequestWorkLogEntity` (by `ServiceRequestAssignmentId`), `ServiceRequestCompletionEntity`
  (`sr.Completion`), `ServiceRequestStatusHistoryEntity`, `ServiceRequestMessageEntity` (system messages
  `JOB_STARTED` / completion), `ServiceRequestAssignmentEntity` (`Status`, `ActualStart/EndDate`).

## Work — a guarded Dev/Local reset (idempotent)

Gate the whole thing on `env.IsDevelopment()` (or the existing seed-enabled flag). All steps no-op if already reset.

1. **Delete test work logs** for assignment `91001` → remove every `ServiceRequestWorkLogEntity` with
   `ServiceRequestAssignmentId == 91001` (also drops their `AttachmentFileId` references; the orphaned storage blobs are
   harmless in dev).
2. **Remove the completion**: delete the `ServiceRequestCompletionEntity` for SR `9011` and clear `sr` completion link
   (`SetCompletion(null)` or the domain's clear path).
3. **Remove lifecycle system messages** on SR `9011`: delete the `JOB_STARTED` and any completion (`JOB_COMPLETED` /
   `COMPLETION_SUBMITTED`) `ServiceRequestMessageEntity` rows (SenderType System). Keep `OFFER_ACCEPTED` (it belongs to the
   Assigned state).
4. **Trim status history** on SR `9011`: remove rows for transitions *after* `Assigned` (InProgress, CompletionSubmitted).
   Keep the Assigned row.
5. **Reset the assignment + SR state**:
   - `assignment.Status` → the Assigned/Accepted state it had post-`CreateAssignment` (before Start). Clear
     `ActualStartDate` / `ActualEndDate`. Keep `ScheduledStart/EndDate`.
   - `sr.Status` → `Assigned`.
6. Idempotent guards: if no work logs, no completion, and `sr.Status == Assigned`, the seeder does nothing.

### 7. Give the accepted offer real line items (fixes "teklif kalemleri gelmiyor")
The accepted offer **90001** was created ad-hoc (SEED_ACCEPTED_JOB) with only `TotalAmount` and **no line items** — that's
why the Job Workspace offer table shows a grand total but no rows. It is **not** in the JSON seed. Seed 3 realistic items
onto offer 90001 (in the offer's existing `CurrencyCode`), then recompute totals with the existing
`OfferCalculationService` (the same service the offer builder uses), and persist:

| # | ItemType | Title | Qty | Unit | UnitPrice | TaxRate |
|---|----------|-------|-----|------|-----------|---------|
| 1 | Labor    | Gövde Temizliği İşçiliği        | 24 | HOUR  | 45  | 0.20 |
| 2 | Product  | International Ultra 300 Antifouling | 15 | LITER | 120 | 0.20 |
| 3 | Service  | Sarf Malzeme Paketi             | 1  | PIECE | 250 | 0.20 |

- Build each via `ServiceRequestOfferItemEntity.Create(offerId: 90001, itemType, title, description?, quantity,
  unitPrice, currencyCode, sortOrder, unitCode, taxRate)`, run them through `OfferCalculationService` to set line
  subtotals/tax/line totals, `offer.ReplaceItems(items)`, and set the offer `Subtotal` / `TaxTotal` / `GrandTotal`
  (Ara Toplam ~3130, KDV %20, Genel Toplam ~3756 in the offer currency — exact figures come from the calc service).
- Also set a short `offer.Description` if empty, e.g. "Teknenin su altı kısmındaki kekamozların temizlenmesi,
  zımparalanması ve seçilen yüksek kaliteli antifouling boyanın iki kat uygulanması işidir. Salma ve dümen bölgeleri
  ekstra özen gerektirir."
- Idempotent: if offer 90001 already has items, skip.

### Optional (dev demo polish) — seed 2 realistic conversation messages
If SR 9011's message thread only has test junk, optionally clear non-system messages and insert two demo messages so the
Job Workspace conversation preview looks realistic (matches the design):
- **Owner → provider** (SenderType Owner, Text): "Selamlar, boya uygulaması öncesi gövde zımpara bittikten sonra bir
  fotoğraf paylaşabilir misiniz? Son durumu görmek isterim."
- **Provider → owner** (SenderType Provider, Text): "Tabii ki, zımpara işlemi şu an devam ediyor. Öğleden sonra temizlik
  bitince detaylı fotoğraf göndereceğim."
Keep the channel open. (Purely cosmetic — skip if you prefer to leave the thread as-is.)

## Acceptance — observed
- `GET /provider/jobs/91001` → `status` **Assigned**, no `actualStart/End`, scheduled dates intact.
- `GET /provider/jobs/91001/work-logs` → **empty** (0 logs).
- SR 9011 has no completion; the `JOB_STARTED` / completion system messages are gone; `OFFER_ACCEPTED` remains.
- `GET /provider/jobs/summary` → `assigned=1, active=1` (completionSubmitted back to 0).
- Re-running the seeder is a no-op.

## Report
Append to `REPORT_BACKEND.md` ("SEED reset 91001"): the job back to Assigned with an empty work-log feed and cleared
completion/lifecycle messages, plus (if done) the two demo conversation messages. Unfinished is **not done**.

## After this (I verify on screen, no new work)
Start the job live (İşi Başlat) → the corrected RT toast should read **"İş güncellemesi"** (System), not "Müşteri size
yanıt verdi"; the İş Kayıtları feed starts clean; the redesigned conversation preview shows the demo messages.
