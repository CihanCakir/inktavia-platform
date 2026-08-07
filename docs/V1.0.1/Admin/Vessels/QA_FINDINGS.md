# Admin QA — Vessels + File Storage (A-QA6)

Routes: `/app/vessels` (+ `/register`, `/:vesselId`, `/:vesselId/documents`), `/app/file-storage`.

## Static findings
**Vessels** — REAL and mostly clean (`useVesselsListQuery`, register wizard, detail; `OperationalStatusBadge` + BFF `*Label`
fields → enums OK; full `t('vessels')` i18n). Issues:
- `VesselsListPage.tsx:93` — **"Export Registry" dead button** (`onClick={() => undefined}`).
- `VesselDocumentsPage` — list REAL (`useVesselDocuments`/`useVesselMedia`) but **actions stubbed**: `handleApprove` /
  `handleReplace` are `console.log('[TODO]…')` (L54,65); "Upload Document"/"Upload Media" buttons have **no onClick** (L86).
  Backend has vessel-documents CRUD (M4e) — these should be wired.

**File Storage** `FileStoragePage` — REAL (`useFilesListQuery` / `useFileDeleteMutation`) but:
- **No i18n** (hardcoded English despite `files.json` existing).
- Minimal: no search / filters / pagination controls.
- `entityType` rendered raw.
- **Orphaned** — not in the real sidebar (only in dead `navigation.ts`).

## Live walkthrough checklist
- [ ] Vessels list: Export Registry works or is removed; register wizard end-to-end; detail loads.
- [ ] Vessel documents: Approve / Replace / Upload actually call the BFF (not console.log).
- [ ] File Storage: reachable from nav; localized; list has search/filter/paging; delete works; entityType labelled.

## Fix candidates
`FIX_A_QA6_VESSEL_DOC_ACTIONS` (wire approve/replace/upload + export), `FIX_A_QA6_FILES_NAV_I18N_CONTROLS`.
