# Branching Strategy (Gitflow)

## Purpose
This repository follows a Gitflow-aligned branching model to ensure:
- Controlled releases to production (`main`)
- Stable integration environment (`develop`)
- Traceable, reviewable changes via Pull Requests
- Clear evidence of governance (branch protections, reviews, CI checks)

## Branches

### main
- Production-ready branch.
- Protected with ruleset: PR required, status checks required, approvals required, no direct pushes.

### develop
- Integration branch for ongoing development.
- Protected with ruleset identical to `main` for discipline and evidence.

### feature/*
- Feature development branches.
- Created from `develop`, merged back to `develop` via PR.

Naming:
- `feature/<short-scope>` or `feature/<area>-<scope>`
Examples:
- `feature/github-governance`
- `feature/ci-build-test`
- `feature/module-skeleton-identity`

### release/*
- Release preparation branches.
- Created from `develop`, merged to `main` via PR, then back-merged to `develop`.

Naming:
- `release/<version>`
Example:
- `release/0.1.0`

### hotfix/*
- Urgent production fixes.
- Created from `main`, merged to `main` via PR, then back-merged to `develop`.

Naming:
- `hotfix/<version>-<scope>`
Example:
- `hotfix/0.0.1-sample-fix`

## Merge Policy
- No direct pushes to `main` or `develop`.
- All changes must go through PR with:
  - Required approvals (mentor/code owner)
  - Required CI checks (build + test)

Merge method:
- Preferred: **Squash merge** for a clean history.
- Rationale: atomic PR history, easy rollback, consistent changelog generation.

## Commit Message Convention
Conventional commits are recommended:
- `feat: ...`
- `fix: ...`
- `chore: ...`
- `docs: ...`
- `refactor: ...`
- `test: ...`
- `ci: ...`

Examples:
- `ci: add build and test workflow`
- `chore(github): auto-assign PR author and request code owner review`
- `docs: add git governance documentation`

## Release Hygiene
- `CHANGELOG.md` updated as part of release preparation.
- Release tags follow `v<version>` (e.g., `v0.1.0`).
