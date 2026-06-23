# Branch Protection Runbook — `main`

Branch protection is configured in the GitHub UI (not versionable as a repo file).
This document records the required settings and the equivalent CLI command so the
configuration is reproducible from this file.

## Required settings for `main`

- Require a pull request before merging
  - Required approving reviews: 1
  - Require review from Code Owners (CODEOWNERS file)
- Require status checks to pass before merging
  - Require branches to be up to date before merging
  - Required checks: `changes`, `ci-gate`
- Do not allow bypassing the above settings (enforce for administrators)
- Allow force pushes: disabled
- Allow deletions: disabled

## Apply via GitHub CLI

Run once after merging this file to `main`:

    gh api repos/JCarlosHidalgo/DAMA/branches/main/protection \
      --method PUT \
      --field 'required_status_checks[strict]=true' \
      --field 'required_status_checks[contexts][]=changes' \
      --field 'required_status_checks[contexts][]=ci-gate' \
      --field 'enforce_admins=true' \
      --field 'required_pull_request_reviews[required_approving_review_count]=1' \
      --field 'required_pull_request_reviews[require_code_owner_reviews]=true' \
      --field 'restrictions=null'

## Verify current state

    gh api repos/JCarlosHidalgo/DAMA/branches/main/protection
