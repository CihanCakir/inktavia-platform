# REPORT — P-QA7: build the Performance + Documents stubs (backends exist)

> Executes `FIX_P_QA7_BUILD_PERFORMANCE_DOCUMENTS.md`. `inktavia-marine-provider-web` — additive, tr+en.
> **Outcome: Documents → built. Performance → hidden** (its only backend is admin-internal scoring, not a
> provider KPI surface — the spec's explicit fallback). No BFF change was needed. **Not committed.**

---

## TL;DR

| Page | Decision | Why |
|------|----------|-----|
| **Documents** | **BUILT** | The provider's document list already ships on `GET /onboarding` (`documents[]`), and the attach/delete/access-url commands already exist. Pure FE consumer of a proven backend. |
| **Performance** | **HIDDEN** (nav item removed) | The only backend is `Modules/Profile`'s **admin-scoped internal priority-scoring** snapshot — abstract decision-support scores, explicitly "does NOT change ranking", gated `Roles=Admin,profile.admin`, which the provider portal does not hold. Not provider-facing KPIs. Per the doc's fallback, hide rather than ship a stub. |

No MarineProvider BFF code was added — see "Why no BFF passthrough" below.

---

## Documents — built

### Backend (reused as-is, no new code)
The document list is **not** a gap: `GET /api/v1/provider/onboarding` already returns
`OnboardingResponse.Documents` (`List<ProviderDocumentDto>` — `fileId`, `fileName`, `contentType`,
`sizeInBytes`, `documentType`, `issuer`, `uploadedAt`, `reviewStatus`, `resolutionNote`). Actions reuse the
existing BFF commands:
- upload → `POST /files/upload-session` → PUT-to-storage (direct, bypasses BFF) → `POST /files/{id}/complete`
  → `POST /onboarding/documents` (AttachOnboardingDocument)
- download → `POST /onboarding/documents/{fileId}/access-url` (GetDocumentAccessUrl → presigned MinIO URL)
- delete → `DELETE /onboarding/documents/{fileId}` (DeleteOnboardingDocument)

So a separate `GetOnboardingDocumentsBff` query was **unnecessary** — the list surface already exists. Building
one would have duplicated the onboarding read. Kept it lean.

### Frontend (new)
- **`src/features/documents/hooks/useProviderDocuments.ts`** — `useProviderDocuments()` (reads the documents
  off the shared `useOnboardingDraft` query — same `['onboarding']` cache key, so upload/delete invalidations
  refresh both this page and the onboarding step), `useDocumentUpload()`, `useDocumentDelete()` mutations. All
  three reuse the existing `@features/onboarding/api/documentsApi` (`uploadAndAttach`, `deleteDocument`,
  `getAccessUrl`) — the same proven upload/attach/delete/access-url module the onboarding Compliance step uses.
- **`src/features/documents/pages/DocumentsPage.tsx`** — replaced the `PlannedPage` stub with a real page:
  - **Upload card** — document-type select (Maritime licence / Insurance certificate / Business registration /
    Other), optional issuer, `UploadDropzone`, client-side pre-checks (PDF/JPEG/PNG, ≤10 MB — the backend
    re-validates), and a phased progress bar (preparing → uploading → finalizing → attaching).
  - **List** — one row per document: file name, `type · size · uploaded date · issuer`, a review-status badge
    (Approved / Under review / Needs review / Rejected → green / amber / amber / red via `StatusBadge`), a
    rejection note when present, a **view** action (mints a signed read URL on click, opens in a new tab, never
    pre-fetched or stored) and a **delete** action.
  - **States** — `LoadingState` while the query is pending, `ErrorState` (with retry) on a failed envelope,
    `EmptyState` when there are no documents, and an inline `AlertBanner` for per-action errors (bad type/size,
    an access-url refusal, a delete failure). Read/write, no economics.
  - Both the empty-body access-url refusal and the "HTTP 200 + `success:false`" delete refusal are handled
    the same defensive way the onboarding page documents.
- **i18n** — `en/tr documents.json` expanded from 2 → **31 keys each** (parity 31/31): title/subtitle, list
  title, upload labels + progress phases, type labels, status labels, actions, empty, load-error, and the four
  per-action error strings.

## Performance — hidden

- **`src/app/router/navigation.ts`** — removed the `performance` nav entry (and its now-unused `TrendingUp`
  import), with an inline comment recording *why*: the only backend
  (`api/v1/profile/admin/performance`, `[Authorize(Roles="Admin,profile.admin")]`) returns
  `ProfilePerformanceSnapshotDto` — `OverallScore`, `RiskPenaltyScore`, `ConfidenceLevel`, `IsColdStart`, … —
  an **internal decision-support score** the controller itself annotates as "does NOT change ranking". It is
  not "completed jobs / rating / response time / acceptance rate" and the provider portal isn't granted
  `profile.admin`. Surfacing it would be both wrong (abstract scores, not provider KPIs) and unauthorized.
- `PerformancePage.tsx` and its `/app/performance` route are left untouched (harmless, now unreachable from the
  nav). Restoring the item is a one-line flip once a real provider-performance endpoint exists — the comment
  says exactly that.

## Nav reconcile
- `documents` → `status: 'implemented'` (YAKINDA badge dropped).
- `performance` → removed from `navItems` (hidden).

---

## Verify (on screen — localhost:3002/app/documents, PROVIDER 2 AS / Cihan Çakır, tr)

- ✅ **Real list, not the stub.** `/app/documents` renders "Belgeler" + subtitle, the upload card, and
  **Yüklenen belgeler (1)** — the provider's real seed doc `inktavia-test-license.pdf`
  (`Denizcilik lisansı · 403 B · 13.07.2026 tarihinde yüklendi`, **İnceleniyor** badge, view + delete).
- ✅ **Upload works (full presigned round-trip).** Uploaded a fresh `pqa7-live-upload.pdf` → the 4-step flow
  ran (create-session → PUT to storage → complete → attach) → list refetched to **(2)** with the new row
  (`200 B · 07.08.2026`, İnceleniyor).
- ✅ **Download works (presigned).** Clicking view on the fresh doc `POST …/access-url` → **200**, and a new
  tab opened at a genuine MinIO **presigned** URL
  (`localhost:9000/inktavia-filestorage-local/document/2026/08/…pdf?X-Amz-Expires=300&X-Amz-Algorithm=AWS4-HMAC-SHA256&…&X-Amz-Signature=…`),
  rendering the PDF.
- ✅ **Error state works.** Clicking view on the seed doc surfaced a clean localized banner "Belge açılamadı.
  Lütfen tekrar dene." — its `access-url` returned 200 with an **empty body** (the seed row has no backing
  object in storage), and the defensive branch caught it instead of crashing. A real uploaded doc (above)
  mints a URL, confirming this is a seed-data gap, not a code defect.
- ✅ **Delete works.** Deleting `pqa7-live-upload.pdf` → list auto-refetched back to **(1)** (original state
  restored — clean; nothing left behind).
- ✅ **Nav reconciled.** Sidebar **FİNANS** group now shows only *Finans* (Performans hidden); **HESAP** shows
  *Belgeler* highlighted with **no YAKINDA badge**.
- ✅ **No console errors** after a fresh load. (Two transient `TrendingUp is not defined` HMR errors appeared
  in the middle of editing `navigation.ts` — import removed one keystroke before its usage — and cleared on
  reload; the final file has zero `TrendingUp` references.)
- ✅ **tsc `--noEmit` = 0, eslint = 0** on the new/changed files; **documents i18n parity 31/31** en↔tr.

## Scope

New: `src/features/documents/hooks/useProviderDocuments.ts`,
`src/features/documents/pages/DocumentsPage.tsx` (was the stub). Changed:
`src/app/router/navigation.ts` (documents→implemented, performance removed + import),
`src/shared/i18n/locales/{en,tr}/documents.json`. **No BFF, no module, no economics.** **Not committed.**

## Known / out of scope
- The shared `UploadDropzone`'s secondary hint line renders a hardcoded English string
  ("PDF, JPG, or PNG. Max size 10MB per file.") under the localized CTA — a **pre-existing** component quirk
  used identically on the onboarding Compliance step; left untouched to avoid a cross-cutting change (the
  page's own localized hint sits just below it).
