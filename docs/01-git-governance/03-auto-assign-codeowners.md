# Auto-Assign & Code Owners

## Objective
Automate PR hygiene while enforcing mentor/code-owner review:
- PR is assigned to the author (ownership & accountability)
- Review is requested from code owners
- Rulesets ensure code owner approval is required to merge

## Auto-assign & reviewer request workflow
Workflow file:
- `.github/workflows/auto-assign-review.yml`

Behavior:
- On PR opened / reopened / ready for review:
  - Assign PR to the PR author
  - Request review from Code Owner / mentor

## CODEOWNERS
File:
- `.github/CODEOWNERS`

Policy:
- Define code owners so that review routing is deterministic.

Example:
- `* @UmitAkinci`

## Ruleset enforcement
To ensure that code owner approval is mandatory:
- Enable **Require review from Code Owners** in rulesets for `main` and `develop`.

## Why this matters
- Formalizes review gate (mentor approval)
- Strengthens “code review / clean code” competency evidence
- Prevents accidental merges with only non-owner approvals

## Evidence
See: `docs/01-git-governance/05-proof-log.md`
