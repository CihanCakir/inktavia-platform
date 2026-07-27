# L12 — Payout receipt PDF (generate + signed read-URL) — Backend Prompt

> **Context:** Inktavia Marine OS, `Aizen.Modules.Payment`. Adds a downloadable **payout receipt (dekont)** PDF for the
> provider Finance → Ödemeler tab (row action + detail drawer). Reuses **L11's** PDF + FileStorage infra
> (`IInvoicePdfRenderer`/QuestPDF setup, `PaymentFileStorageService`, `IFileStorageRemoteCall`, `CreateReadUrl`).
> Do L12 **after** L11 (shared plumbing). Read-only from the provider's side.

## Anchors
- Payout record (the entity behind PAY-1 `ProviderPayoutDto`): provider-scoped by `ProviderProfileId`; fields incl.
  amount, currency, status, gatewayProvider, gatewayPayoutId, sourceType/sourceId, requestedAt, processedAt.
  Identify its repo `GetByIdAsync` + provider filter (mirror PAY-1).
- **Add a nullable `ReceiptFileRef` (maxLen 500)** column to the payout entity (+ EF config + migration) for idempotent
  storage — same role as `InvoiceHeaderEntity.PdfFileRef`.
- `PaymentProviderController` + `ResolveProviderProfileId()` exist.

## Steps
1. **Renderer:** `IPayoutReceiptPdfRenderer.Render(payout) : byte[]` (QuestPDF, reuse L11 setup) — a receipt: provider
   name/profile, payout amount + currency (large), status, gateway + reference, requested/processed dates, and the
   source (e.g. "CargoDry settlement payout"). Turkish labels.
2. **Ensure service:** `EnsureReceiptAsync(payoutId) : fileId` — if `ReceiptFileRef` set → return it; else render + store
   via FileStorage session flow (reuse the **now-working** `PaymentInvoicePdfService` pattern), `SetReceiptRef(fileId)` +
   save. Idempotent.
   > **Reuse L11's solved plumbing exactly — do not reinvent it:** (a) the remote-call interface must be envelope-aware
   > (`Task<AizenApiResponse<FileUploadSessionDto>>`, unwrap `.Body`); (b) set **`ServerSideUpload = true`** on
   > `CreateUploadSessionRequest` so FileStorage signs the presigned URL with the INTERNAL endpoint (`minio:9000`);
   > (c) PUT the presigned URL **UNCHANGED** — **no host-rewrite** (SigV4 signs `host`; rewriting → 403). Validate the URL
   > is absolute; then `CreateReadUrl` (envelope-unwrap) for the returned link.
   > Only generate a receipt for a **Completed** payout (status Completed=3). For non-completed, the endpoint returns
   > 409/`RECEIPT_NOT_READY` (no receipt for pending/failed).
3. **Query + endpoint:** `GetProviderPayoutReceiptUrl { ProviderProfileId; PayoutId } : ProviderFilePdfUrlDto?`
   (reuse L11's DTO). Access-check `payout.ProviderProfileId == pid` else 404; status guard (2); `EnsureReceiptAsync`
   → `CreateReadUrl(5min)`. Controller:
   ```
   GET /api/v1/payment/provider/payouts/{id:long}/receipt-url  → AizenApiResponse<ProviderFilePdfUrlDto>
   ```
   (404 not owned, 409 not completed). Typed + PRT; mint per click.
4. **BFF:** `IPaymentRemoteCall.GetPayoutReceiptUrl(long id)` → DTO; BFF query + `GET
   /api/v1/provider/payment/payouts/{id}/receipt-url`, typed + PRT.

## Verify — PASTE output
provider2 = **100011** has a **Completed** payout (PAY-1 seed: 40 TRY, "CargoDry settlement payout — completed").
1. Build + migration applied; restart services.
2. HTTP (provider2 token):
   ```
   GET /api/v1/provider/payment/payouts/{completedId}/receipt-url  # 200 → { url, expiresInSeconds }
   curl -s "<url>" -o /tmp/rcpt.pdf && file /tmp/rcpt.pdf           # → "PDF document"
   GET .../payouts/{pendingId}/receipt-url                          # 409 RECEIPT_NOT_READY
   GET .../payouts/{foreignId}/receipt-url                          # 404
   ```
   Paste JSON, `file` result, `ReceiptFileRef` null→fileId, and the 409/404 cases.

## Acceptance
- Completed payouts expose a signed receipt-PDF URL (idempotent generate+store, access-checked, status-guarded);
  non-completed → 409; foreign → 404; typed + PRT; URL serves a valid PDF. Build + migration clean; evidence pasted.
  FE then enables the payout row "Dekont indir" action + a button in the payout detail drawer.

## Report
`REPORT_BACKEND.md` ("L12"): payout receipt renderer + ensure-store (ReceiptFileRef migration) + provider receipt-url
endpoint (owned + completed-only) + BFF. Verified: url→PDF, 409 pending, 404 foreign. Completes the Finance Large-phase
backend (with L11).
