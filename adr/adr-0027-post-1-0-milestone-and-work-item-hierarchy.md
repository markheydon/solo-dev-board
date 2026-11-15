# ADR-0027: Post-1.0 milestone and work-item hierarchy

**Date:** 2026-08-18
**Status:** Active

## Context

After `v1.0.0`, delivery was still described both as implementation phases and as GitHub milestones that sometimes copied a product theme. This record was migrated from legacy decision log entry DEC-027 during the repository workflow alignment chore (2026-11-14).

On 2026-11-15 the body was restated. It had accumulated a release-by-release status log (which milestone was open, which tag had shipped, and one-off scope folds such as branding). That log is not the decision. Shipped milestone names and tags stay in GitHub and in [`plan/RELEASE_PLAN.md`](../plan/RELEASE_PLAN.md). Do not append release status to this file.

## Decision

After `v1.0.0`:

1. Keep **at most one open GitHub milestone**. A milestone is a release target, not an ongoing product theme. Open the next milestone when that release is scoped. Until then, further work stays unmilestoned. Do not hold several open milestones, and do not recreate a milestone whose only job is to rename a theme that already shipped.

2. Use **Epic → Feature → Story** only when each level names real product structure. Link parents with GitHub sub-issues.
   - An **epic** is a shippable theme that spans multiple features. It is not a bucket that mirrors the milestone.
   - A **feature** is a user-facing capability that groups two or more stories or enablers. Do not invent a feature for a single story. Catch-up exception: [#272](https://github.com/markheydon/solo-dev-board/issues/272) is a feature with no parent epic.
   - A **story** is a discrete delivery unit with acceptance criteria.
   - A story that finishes an already-shipped feature may sit on the milestone without a new feature parent. Add a feature parent when planning a new capability, not beforehand.

3. Ice-box work and platform-blocked work stay unmilestoned until they are explicitly promoted.

4. Implementation phases in [`plan/archive/IMPLEMENTATION_PLAN.md`](../plan/archive/IMPLEMENTATION_PLAN.md) are historical sequencing for the v1.0 release. The Project board **Phase** field is legacy for closed pre-1.0 milestones only.

## Consequences

- **POS-001**: GitHub is the place that answers which release is open, and which issues belong to it.
- **POS-002**: This record stays valid when a tag ships, so it does not need a revision per release.
- **NEG-001**: A release-specific exception (scope pulled forward, a milestone closed early, a numbering one-off) is not recorded here. Put that in the release plan, or write a new ADR if the rule itself changes.

## Notes

See git history of `plan/DECISIONS.md` for the pre-migration rationale. [ADR-0024](adr-0024-post-1-0-milestone-numbering.md) is superseded by this decision.
