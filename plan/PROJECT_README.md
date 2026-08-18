# SoloDevBoard Roadmap (Project #8 info pane)

Canonical copy of the GitHub Project **SoloDevBoard Roadmap** README (the text in the [info pane](https://github.com/users/markheydon/projects/8?pane=info)).

This file is **not** applied automatically. After editing it, copy the fenced **Info pane README** block (the markdown inside the code fence) and paste it into Project #8 → Info.

Refresh this file during each [PM progress review](../.agents/workflows/pm-progress-review.md) so the public board description does not lag the milestones.

---

## Info pane README

```markdown
## SoloDevBoard Roadmap

SoloDevBoard provides a single pane of glass for solo developers managing GitHub workloads across multiple repositories.

**Status:** On track. Public release [v1.0.0](https://github.com/markheydon/solo-dev-board/releases/tag/v1.0.0) tagged 17 August 2026.

### Current focus

v1.1.0 — deferred follow-ons ([#289](https://github.com/markheydon/solo-dev-board/issues/289)).

Phases 1–4 and Phase 6 are complete. The six shipped tools plus hosted and self-host hardening are in production. Next work is the parked Phase 1–4 slices (label consistency warnings, project board column migration, custom workflow template repositories) and any dogfood fixes from the public tag. Private user-owned Projects v2 under hosted sign-in remains blocked ([#293](https://github.com/markheydon/solo-dev-board/issues/293)). Cross-Repo PM Workflow (Phase 5 / v1.2.0) stays parked until v1.1.0 is underway.

### Roadmap

| Phase | Milestone | Goal | Status |
|---|---|---|---|
| Phase 1 | v0.1.0 | Foundation — auth, repositories, dashboard shell. | Complete. |
| Phase 2 | v0.2.0 | Label Manager + Audit Dashboard. | Complete. |
| Phase 3 | v0.3.0 | One-Click Migration + Triage UI. | Complete. |
| Phase 4 | v0.4.0 | Board Rules Visualiser + Workflow Templates. | Complete. |
| Phase 6 | v1.0.0 | Production Ready — hosted validation and public release. | Complete. |
| Phase 6 follow-ons | v1.1.0 | Deferred Phase 1–4 slices and dogfood fixes. | Not started. |
| Phase 5 | v1.2.0 | Cross-Repo PM Workflow. | Parked. |

Do not tag `v0.5.0` now that `v1.0.0` exists ([DEC-024](https://github.com/markheydon/solo-dev-board/blob/main/plan/DECISIONS.md#dec-024-post-10-milestone-numbering)).

### Current snapshot

Snapshot date: 18 August 2026.

| Milestone | Closed | Open | Complete |
|---|---:|---:|---|
| v0.1.0 | 9 | 0 | 100% |
| v0.2.0 | 73 | 0 | 100% |
| v0.3.0 | 38 | 0 | 100% |
| v0.4.0 | 25 | 0 | 100% |
| v1.0.0 | 78 | 0 | 100% |
| v1.1.0 | 0 | 5 | 0% |
| v1.2.0 | 0 | 17 | Parked |

v1.2.0 still has 12 closed duplicate issues (#260–#271) on the GitHub milestone. The live Phase 5 queue is #272–#288 (17 open). The table uses the live queue, not the duplicate closed count.

### Key resources

- [Repository](https://github.com/markheydon/solo-dev-board)
- [Implementation plan](https://github.com/markheydon/solo-dev-board/blob/main/plan/IMPLEMENTATION_PLAN.md)
- [GitHub Issues](https://github.com/markheydon/solo-dev-board/issues)
- [Progress reviews](https://github.com/markheydon/solo-dev-board/tree/main/plan/weekly-updates)
- [v1.0.0 release](https://github.com/markheydon/solo-dev-board/releases/tag/v1.0.0)
- [Product site](https://solodevboard.com/)
- [User Guide](https://solodevboard.com/docs/)
```
