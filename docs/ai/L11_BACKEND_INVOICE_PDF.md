# L11 — Invoice PDF download (generate + signed read-URL) — Backend Prompt

> **Context:** Inktavia Marine OS, `Aizen.Modules.Payment`. The provider Finance → Faturalar tab lists invoices and
> opens a detail drawer; PDF download is currently deferred (`HasPdf` only). This adds real PDF download:
> **generate the invoice PDF on demand, store it in FileStorage, and hand the provider a short-lived signed read-URL.**
> Builds on PAY-4 (provider invoices, anchor `BuyerUserId == providerProfileId`, provider-type whitelist) and the
> existing FileStorage remote-call infra.

## Verified anchors
- `InvoiceHeaderEntity`: `PdfFileRef?` (nullable, maxLen 500) + `SetPdfRef(string)`; full header + `Lines`
  (`InvoiceLineEntity`) + `TaxBreakdown` (`InvoiceTaxBreakdownEntity`); `GetByIdFullAsync(id)` loads all.
- Provider access rule (reuse PAY-4): `invoice.BuyerUserId == providerProfileId` **and** InvoiceType ∈
  { CommissionInvoice=2, SubscriptionInvoice=3, ProviderSettlementStatement=8 } — else 404.
- **No PDF library is installed** → add one (see step 1).
- FileStorage cross-module abstraction `IFileStorageRemoteCall`: `CreateUploadSession`, `CompleteUploadSession`,
  `CreateReadUrl(fileId, { ExpiresIn }, bearer)`, `GetFileMetadata`, `LinkFileToOwner`. Vessel uses it via a thin
  `VesselFileStorageService` — mirror that pattern with a `PaymentFileStorageService`.
- `PaymentProviderController` (PAY-0) with `ResolveProviderProfileId()` exists.

## 1) PDF generation
- Add **QuestPDF** (Community license) to `Aizen.Modules.Payment.Application` (or a dedicated
  `Aizen.Modules.Payment.Pdf` project). Set `QuestPDF.Settings.License = LicenseType.Community` at startup.
- `IInvoicePdfRenderer` / `InvoicePdfRenderer.Render(InvoiceHeaderEntity inv) : byte[]` — an A4 document:
  seller/buyer blocks, invoice number + type + issue/due dates + status, a lines table (desc/qty/unit/line total/VAT),
  totals (subtotal, discount, VAT, grand total, paid, remaining), currency, notes. Turkish labels; UTF-8 fonts.
  Keep layout simple and deterministic (no external assets/network).

## 2) Ensure-PDF service (idempotent generate + store)
`PaymentInvoicePdfService.EnsurePdfAsync(long invoiceId, CancellationToken) : string /*fileId*/`
- Load invoice full. If `PdfFileRef` already set → return it (idempotent; no regeneration).
- Else render bytes (step 1), store into FileStorage:
  `CreateUploadSession` (contentType `application/pdf`, a stable name e.g. `invoice-{InvoiceNumber}.pdf`) → PUT the
  bytes to the returned presigned upload URL → `CompleteUploadSession` → take the resulting **fileId**.
  Optionally `LinkFileToOwner` (owner = Payment/Invoice, ownerId = invoiceId). Then `inv.SetPdfRef(fileId)` + save.
  Guard against duplicate generation under concurrency (idempotency by invoiceId; a set `PdfFileRef` short-circuits).

## 3) Query + endpoint (provider-scoped)
- Query `GetProviderInvoicePdfUrl { long ProviderProfileId; long InvoiceId } : ProviderFilePdfUrlDto?`
  Handler: `GetByIdFullAsync`; **access-check** (BuyerUserId == pid && type whitelist) else null → 404;
  `EnsurePdfAsync(invoiceId)` → fileId; `CreateReadUrl(fileId, ExpiresIn=5min)` → url. Return `{ Url, ExpiresInSeconds }`.
- DTO `ProviderFilePdfUrlDto { string Url; int ExpiresInSeconds; }` in Payment.Abstraction.
- Controller (`PaymentProviderController`):
  ```
  GET /api/v1/payment/provider/invoices/{id:long}/pdf-url  → AizenApiResponse<ProviderFilePdfUrlDto>  (404 if not owned)
  ```
  Mint per click — never prefetch for a list. Typed + PRT.

## 4) BFF passthrough
- `IPaymentRemoteCall`: `GetInvoicePdfUrl(long id) : AizenApiResponse<ProviderFilePdfUrlDto>`.
- BFF query `GetProviderInvoicePdfUrlBff` (resolver → `.Body`); BFF `PaymentController`:
  `GET /api/v1/provider/payment/invoices/{id}/pdf-url`, typed + PRT.

## Verify — run and PASTE output (container + DB; do not report done until all pass)
provider2 = **100011**; provider2 has ≥1 provider invoice (seeded in PAY-4, `PST-DEV-2026-0001`).
1. Build module + BFF: 0 errors; QuestPDF restores; restart `payment-api` + `bff-marineprovider`.
2. HTTP smoke (provider2 token):
   ```
   GET /api/v1/provider/payment/invoices/{ownId}/pdf-url     # 200 → { url, expiresInSeconds }
   curl -s "<returned url>" -o /tmp/inv.pdf && file /tmp/inv.pdf   # → "PDF document"
   GET .../invoices/{ownId}/pdf-url                          # 2nd call: PdfFileRef reused, no regeneration
   GET .../invoices/{foreignId}/pdf-url                      # 404 (not owned / renewal invoice)
   ```
   Paste: the JSON, the `file` result (confirms a real PDF), the DB `PdfFileRef` before/after (null → fileId), and the
   404 for a foreign invoice.

## Acceptance
- `GET /provider/payment/invoices/{id}/pdf-url` generates (once, idempotent) + stores the invoice PDF in FileStorage and
  returns a short-lived signed read-URL; access-checked (404 for non-owned / non-whitelisted); typed + PRT; the URL
  serves a valid PDF. Build clean; evidence pasted. FE then flips the drawer + row "PDF indir" from disabled to a real
  download (open `url` in a new tab).

## Report
`REPORT_BACKEND.md` ("L11"): invoice PDF renderer (QuestPDF), ensure-PDF store service (FileStorage session flow +
SetPdfRef, idempotent), provider pdf-url query/endpoint (access-checked), BFF passthrough. Verified: url→valid PDF,
reuse on 2nd call, 404 isolation. Next: L12 payout receipt PDF (same infra).
