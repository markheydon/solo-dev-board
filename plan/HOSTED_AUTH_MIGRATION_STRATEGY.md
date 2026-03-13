# Hosted Authentication Migration Strategy

Date: 2026-03-13.  
Issue: #118.  
Applies to: Feature #103 (ADR-0015).

## Purpose

Lock the delivery path from the superseded hybrid hosted-authentication direction to the GitHub App-first target architecture before coding continues on Feature #103.

## Superseded vs Target Slices

| Area | Superseded Hybrid Direction | GitHub App-First Target | Migration Action |
|---|---|---|---|
| Hosted sign-in entrypoint | Parallel GitHub App and separate OAuth App hosted sign-in path. | GitHub App user authentication is the primary hosted sign-in path. | Remove hybrid-first planning assumptions and implement App-first by default. |
| OAuth App dependency | Planned as a first-class hosted dependency. | Fallback-only for unmet hosted sign-in requirements. | Demote OAuth App work behind explicit fallback criteria and track as conditional scope (#113). |
| User context model | Hosted session shape expected to support OAuth-centric flow and later App linkage. | Per-request hosted user context derived from GitHub App-first sign-in and installation mapping. | Implement user context directly against App-first hosted claims and installation context (#112, #111). |
| Secret/config posture | Mixed expectation of OAuth and App credentials in hosted configuration baseline. | Hosted configuration prioritises GitHub App credentials and admission control inputs; PAT remains local trusted mode only. | Re-baseline hosted configuration to App-first requirements and keep PAT-only mode isolated to local trusted environments. |
| Rollout sequencing | Hybrid implementation branch could be merged incrementally into new work. | Explicit migration gate before hosted auth coding, with controlled fallback plan. | Freeze hybrid branch as reference-only and continue delivery only on the locked App-first plan branch strategy. |

## Branch Strategy Lock

The superseded hybrid implementation path is frozen and treated as reference-only.

1. New hosted-auth implementation work proceeds only on issue-scoped feature branches from current main.
2. No direct merge from the old hybrid branch is permitted.
3. Cherry-picking from superseded work is allowed only when all checks pass:
   - The commit is implementation-agnostic and does not enforce hybrid auth behaviour.
   - The commit preserves ADR-0015 direction (GitHub App-first, OAuth fallback-only).
   - The commit preserves PAT-only local trusted mode.
   - The commit is reviewed and referenced in the destination issue or pull request notes.
4. If a commit fails any check, reimplement it directly on the new branch path rather than importing it.

For this issue, work is locked to branch feature/issue-118-lock-branch-and-migration-strategy.

## Hosted Configuration and Secret Migration Expectations

1. Hosted deployments continue to source secrets from Azure Key Vault-backed app settings, never from committed files.
2. Hosted baseline for Feature #103 prioritises GitHub App authentication material and admission control configuration.
3. Existing Key Vault-backed PAT reference remains only for local trusted mode compatibility and operational continuity until the hosted cutover completes.
4. OAuth App secret material is not required for the default hosted path; introduce it only if explicit fallback criteria are met.
5. Any new hosted auth settings introduced during implementation must be documented in:
   - docs/getting-started.md.
   - infra/README.md.

## PAT-Only Local Trusted Mode Compatibility

1. Local trusted mode remains supported and unchanged as a first-class development path.
2. PAT configuration keys and existing local user-secrets flow remain valid during hosted auth migration.
3. Hosted auth implementation must not introduce runtime coupling that blocks PAT-only local execution.
4. Validation for each hosted-auth slice must include a local PAT-mode smoke check.

## Rollout and Fallback Expectations

1. Rollout order for Feature #103 remains:
   - #111 installation and token lifecycle.
   - #112 user sign-in and per-request user context.
   - #117 hosted admission mapping.
   - #113 OAuth dependency demotion and fallback constraints.
   - #114 authentication coverage.
   - #119 documentation updates.
2. Hosted rollout is App-first by default and deny-by-default for admission control.
3. Fallback to OAuth App is permitted only when a validated hosted requirement cannot be met by GitHub App user authentication.
4. Any fallback activation must include:
   - Explicit issue-level rationale.
   - Updated scope and implementation-plan notes.
   - Updated operator documentation.

## Completion Criteria for Migration Gate

Issue #118 is complete when:

1. This migration strategy is committed and linked from planning artefacts.
2. Issue #118 and roadmap status are in-progress with actual start date recorded.
3. Parent feature and epic status are aligned to in-progress for active delivery.
4. Feature #103 implementation proceeds only under this locked strategy.