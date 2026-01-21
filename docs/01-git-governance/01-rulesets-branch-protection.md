# Rulesets & Branch Protection

## Objective
Enforce governance on critical branches (`main`, `develop`) to support:
- PR-only workflow
- Mandatory review (mentor/code owner)
- Mandatory CI checks (build/test)
- No force pushes / no accidental destructive operations

## Protected Branches
- `main`
- `develop`

## Ruleset: protect-main
### Branch targeting
- Applies to: `main`

### Minimum enforcement
- Require a pull request before merging
- Require approvals (min: 1)
- Require review from Code Owners
- Require status checks to pass (build + test)
- Block force pushes
- Restrict deletions
- Restrict updates (no direct pushes)

### Notes
- “Restrict updates” ensures `main` cannot be updated except via PR merge.
- If merge is blocked due to restrict-updates, a bypass policy is required.

## Ruleset: protect-develop
### Branch targeting
- Applies to: `develop`

### Minimum enforcement
- Require a pull request before merging
- Require approvals (min: 1)
- Require review from Code Owners
- Require status checks to pass (build + test)
- Block force pushes
- Restrict deletions
- Restrict updates (no direct pushes)

## Bypass Policy (when Restrict updates is enabled)
When `Restrict updates` is active, GitHub may require a bypass entry to allow merges.

Recommended bypass:
- Add bypass for **Repository admin (Role)**

Rationale:
- Keeps direct pushes blocked for standard contributors
- Allows controlled merges when PR requirements are met

## Required Status Checks
- `build`
- `test`

These are provided by `.github/workflows/ci.yml`.

## Evidence
See: `docs/01-git-governance/05-proof-log.md`
