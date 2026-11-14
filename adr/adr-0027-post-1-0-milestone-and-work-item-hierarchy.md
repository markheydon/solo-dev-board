# ADR-0027: Post-1.0 milestone and work-item hierarchy

**Date:** 2026-08-18
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-027 during the repository workflow alignment chore (2026-11-14).

## Decision

After `v1.0.0`, maintain **one open GitHub milestone** at a time. **`v1.3 - Usable solo workflow across repositories`** is the current open milestone (tag **`v1.3.0`** when shipped). Locked-in start: Label Manager remap extras ([#491](https://github.com/markheydon/solo-dev-board/issues/491)); further issues will be added from dogfood. **`v1.2 - Planning polish, Reload & Templates`** is closed (tag [`v1.2.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.2.0), 2026-09-07). **`v1.1 - Cross-Repo Planning & Refinement`** is closed (91 closed, 0 open; tag [`v1.1.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.1.0), 2026-08-31). Delivered `v1.2` scope: [#403](https://github.com/markheydon/solo-dev-board/issues/403) (board-scoped Daily Focus), Reload epic [#447](https://github.com/markheydon/solo-dev-board/issues/447) with [#449](https://github.com/markheydon/solo-dev-board/issues/449)–[#451](https://github.com/markheydon/solo-dev-board/issues/451), HttpClient split [#453](https://github.com/markheydon/solo-dev-board/issues/453), custom template repositories [#292](https://github.com/markheydon/solo-dev-board/issues/292), and Triage action-surface usability [#492](https://github.com/markheydon/solo-dev-board/issues/492). Window-focus refetch [#450](https://github.com/markheydon/solo-dev-board/issues/450) stays unmilestoned and ice-boxed. Do not recreate a separate milestone whose only purpose is Cross-Repo Planning — that increment shipped under `v1.1` (the brief unused v1.2.0 planning-only milestone was deleted on 2026-08-18). Platform-blocked work ([#293](https://github.com/markheydon/solo-dev-board/issues/293), [#391](https://github.com/markheydon/solo-dev-board/issues/391), [#498](https://github.com/markheydon/solo-dev-board/issues/498)) and ice-box catalogue work stay unmilestoned unless explicitly promoted. **Epic → Feature → Story** applies when each level names real product structure: Epics are shippable themes spanning multiple features (not milestone buckets); Features group related stories/enablers ([#272](https://github.com/markheydon/solo-dev-board/issues/272) is a catch-up exception — classified as a feature without a parent epic); Stories are delivery units. Use GitHub sub-issues for parent links. During catch-up, milestone stories may sit without Feature parents when they complete shipped features ([#40](https://github.com/markheydon/solo-dev-board/issues/40), [#88](https://github.com/markheydon/solo-dev-board/issues/88), etc.). Implementation **phases** in `IMPLEMENTATION_PLAN.md` are historical sequencing for the v1.0 release only; the Project board **Phase** field is legacy for closed pre-1.0 milestones. Reject bucket epics that duplicate milestones, fabricated Features, and multiple concurrent open milestones.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
