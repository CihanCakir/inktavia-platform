# 04 — Current Provider Frontend Audit

Project: `inktavia-marine-provider-web/` — React 19 + Vite + TS.

## Reusable, already in place

| Concern | Implementation |
|---|---|
| Routing | `react-router-dom@7`; the detail route **exists**: `service-requests/:id → ServiceRequestDetailPage` |
| Server state | `@tanstack/react-query@5`; `queryKeys.serviceRequests.*` incl. `detail(id)` |
| HTTP | authenticated `httpClient` (+ `authInterceptors`), envelope → `ApiResult`; `publicHttpClient` for public only |
| Forms / validation | `react-hook-form` + `zod` |
| i18n | `i18next`; enum→label maps in `features/service-requests/model/serviceRequestEnums.ts` |
| Realtime | `@microsoft/signalr@10`, `useProviderRealtime` + `realtimeContext`; dedupe + coalesce (from discovery work) |
| Money / date / distance formatters | `formatCurrency`, `formatDate`, `formatDistanceKm`, `formatRelativeTime` |
| Map | **MapLibre GL** + OpenFreeMap (added for discovery) — reuse for the detail "Konum" map |
| File upload | onboarding signed-URL uploader (`features/onboarding/api/documentsApi.ts`) — the read/preview side is the pattern for attachments |
| UI kit | `shared/ui/*` incl. form fields, `TextInput`, `SelectField`, `TextAreaField` |
| Design tokens | Nautical Heritage (`tokens.css`, `@theme`) — same as the mock |

## What exists but is thin — `ServiceRequestDetailPage.tsx`

Today it renders the request header + a **single-line offer form** ("price + note", one `ITEM_TYPE_SERVICE` line
built implicitly). This is the exact thing the mock replaces: it must become the **detail + itemized builder**.
The page already correctly handles the envelope's nested `body.detail.request`, the "200 + success:false"
rejection, and the "not found = access denied" collapse — keep those.

## What does not exist

- No itemized offer **table** component (add/edit/delete/reorder rows, per-line tax/total).
- No **money input** with decimal handling, no **quantity** input with unit, no tax-rate input.
- No **auto-save** / unsaved-changes guard / duplicate-submit guard / idempotency-key plumbing.
- No **offer preview** (customer-facing render), no **submit confirmation**, no read-only submitted state, no
  **version history**.
- No **activity timeline** component, no **request message/question** thread UI.
- No **attachment gallery** with signed-read-on-click.
- No sticky quick-summary panel layout.

## Constraints carried from prior work (do not regress)

- CSS resets stay in `@layer base`. Realtime is a hint, not a record. Rejection can be HTTP 200 + `success:false`.
  Never render a raw enum. No `providerProfileId` from the client. `?access_token=` only for the hub. No token in
  app `localStorage`.
- **No budget** anywhere (decision 2026-07-14).
- Money is formatted locale-aware; **the browser's provisional total is UX only** — the screen must show the
  server's total after save/submit.
