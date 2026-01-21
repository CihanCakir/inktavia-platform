# Auto-Merge for develop (after approvals + checks)

## Objective
Enable a controlled auto-merge only for PRs targeting `develop`, so that:
- Developers focus on delivery
- Merges happen automatically once all ruleset conditions are satisfied
- Production branch (`main`) remains manually controlled

## Scope
- Applies only to PRs with base branch: `develop`
- Does not apply to `main`

## Workflow
File:
- `.github/workflows/auto-merge-develop.yml`

Trigger:
- `pull_request_review` submitted event

Condition:
- Review state is `approved`
- PR base branch is `develop`
- PR is not a draft

Action:
- Enables GitHub Auto-merge for the PR (merge method: squash)

## Safety model
This workflow **does not bypass rulesets**.
It only enables “auto-merge”, and GitHub completes the merge when:
- Required checks pass (build + test)
- Required approvals are satisfied
- Code owner review requirement is satisfied

If code owner approval is missing:
- Auto-merge remains pending and merge will not occur.

## Prerequisites
Repository settings:
- Settings → Pull Requests → **Allow auto-merge** enabled
- Squash merge allowed

Ruleset settings for develop:
- PR required
- Approvals required
- Require review from Code Owners
- Required status checks: build + test

## Evidence
See: `docs/01-git-governance/05-proof-log.md`
