# Provider QA — Jobs (P-QA4)

## Static findings
Implemented (`jobs` api+hooks, 2 pages: list + `/:id` detail). The job workspace (JD-1..JD-5: detail aggregate, start/
complete, work-logs, evidence). Needs live-QA.

## Live walkthrough checklist (localhost:3002/app/jobs)
- [ ] Jobs list loads (loading/empty/error); status badges correct.
- [ ] Job detail: aggregate loads; start/complete actions respect ownership + state guards.
- [ ] Work-logs add/list; before/after evidence upload + render.
- [ ] Completion submit → status transition; auto-approve countdown visible if applicable (N3).
- [ ] Sidebar panels (JobSidebarPanels) render real data; no console errors.
