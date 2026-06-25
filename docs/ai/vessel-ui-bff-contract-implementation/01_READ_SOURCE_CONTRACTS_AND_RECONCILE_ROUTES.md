# 01 — Read Source Contracts and Reconcile Routes

Read:

- `reference/SOURCE_VESSEL_BFF_API_CONTRACT.md`
- `reference/SOURCE_VESSEL_BFF_IMPLEMENTATION_PROMPT.md`
- all files under `reference/`

Then inspect current AdminPanel BFF routing.

Tasks:

1. Find current BFF route prefix.
2. Find existing `AdminVesselsController` or equivalent.
3. Decide whether to extend existing controller or add a dedicated Vessel controller.
4. Map each source `/bff/vessels` endpoint to current route convention.
5. Create `docs/reports/vessel-ui-contract-audit-report.md` with endpoint mapping and MVP/Post-MVP priority.

Do not implement code in this step.
