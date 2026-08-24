# SoloDevBoard - Label Strategy

<!-- AI Collaborator Instructions: See the "AI Collaborator Instructions" section at the bottom of this file for guidance on when to apply each label. When creating new issues or PRs, always apply at least one label from each of the "type/" and "priority/" groups. -->

This document defines the canonical label taxonomy for all SoloDevBoard GitHub repositories. All labels should be created using the definitions below to ensure consistency.

For instructions on how to create these labels in bulk, see [PROJECT_MANAGEMENT.md](PROJECT_MANAGEMENT.md). For pull request titles, template usage, and PR metadata, see [PULL_REQUEST_POLICY.md](PULL_REQUEST_POLICY.md).

---

## Label Taxonomy

### Type Labels

Describes the nature of the issue or PR.

| Label | Colour | Description |
|-------|--------|-------------|
| `type/epic` | `#6f42c1` | A named product theme spanning multiple features or a major increment - not a milestone bucket |
| `type/feature` | `#0075ca` | A Feature - groups related stories within an epic |
| `type/story` | `#1d76db` | A user-facing Story delivering a discrete piece of value |
| `type/enabler` | `#e4e669` | An Enabler - technical prerequisite that unblocks stories |
| `type/test` | `#bfd4f2` | A Test issue - test coverage deliverable (unit, component, integration) |
| `type/bug` | `#d73a4a` | A bug or unexpected behaviour |
| `type/chore` | `#fef2c0` | Maintenance, dependency updates, or technical debt |
| `type/documentation` | `#0052cc` | Documentation additions or improvements |

---

### Priority Labels

Describes the urgency and importance of the issue.

| Label | Colour | Description |
|-------|--------|-------------|
| `priority/critical` | `#b60205` | Blocking - must be resolved immediately |
| `priority/high` | `#d93f0b` | Should be addressed in the current sprint or release |
| `priority/medium` | `#fbca04` | Should be addressed soon but is not blocking |
| `priority/low` | `#c2e0c6` | Nice to have; can be deferred |

---

### Status Labels

Describes the current state of the issue or PR in the workflow.

| Label | Colour | Description |
|-------|--------|-------------|
| `status/todo` | `#ffffff` | Ready to be worked on; not yet started |
| `status/in-progress` | `#0e8a16` | Currently being worked on |
| `status/blocked` | `#e11d48` | Cannot proceed; waiting on something external |
| `status/ice-box` | `#8b949e` | Shelved for later; not in the active delivery queue |
| `status/in-review` | `#1d76db` | Pull request open; awaiting code review |
| `status/done` | `#cfd3d7` | Completed and closed |

---

### Feature Area Labels

Describes which feature area the issue relates to.

| Label | Colour | Description |
|-------|--------|-------------|
| `area/dashboard` | `#bfd4f2` | Audit Dashboard feature |
| `area/repositories` | `#c5def5` | Repositories catalogue feature |
| `area/migration` | `#d4c5f9` | One-Click Migration feature |
| `area/labels` | `#c5def5` | Label Manager feature |
| `area/board-rules` | `#fef2c0` | Board Rules Visualiser feature |
| `area/triage` | `#f9d0c4` | Triage UI feature |
| `area/actions-templates` | `#c5def5` | Actions Templates feature |
| `area/planning` | `#96d8c9` | Planning feature — Daily Focus, Backlog, Iteration, Repos |
| `area/infrastructure` | `#e4e669` | Azure infrastructure, CI/CD, deployment |
| `area/docs` | `#0052cc` | Documentation, user guides, ADRs, planning docs |

---

### Size Labels

Provides an estimate of the effort required. Use story points or t-shirt sizing as appropriate.

| Label | Colour | Description |
|-------|--------|-------------|
| `size/xs` | `#dde8c9` | Trivial - less than 1 hour (e.g. typo fix, config change) |
| `size/s` | `#c5def5` | Small - less than half a day |
| `size/m` | `#fef2c0` | Medium - half a day to one day |
| `size/l` | `#f9d0c4` | Large - two to three days |
| `size/xl` | `#d4c5f9` | Extra-large - more than three days; consider splitting |

---

## Creating Labels in Bulk

To create all labels in a repository, you can use the GitHub CLI:

```bash
# Example: create the type/story label
gh label create "type/story" --color "1d76db" --description "A user-facing Story delivering a discrete piece of value" --repo <owner>/<repo>
```

A script to create all labels at once will be provided in `infra/scripts/create-labels.sh` (planned for Phase 1).

---

## AI Collaborator Instructions

### When to Apply Each Label

#### `type/` - Always required on issues and PRs
- Apply `type/epic` to issues that name a shippable product theme spanning multiple features or a major increment (for example Planning). Do not create epics that duplicate milestones or catch unrelated deferred work.
- Apply `type/feature` when **two or more** related stories/enablers deliver one user-facing capability under an epic.
- Apply `type/story` to user-facing delivery issues (e.g. `[Story] Implement REST API calls`).
- Apply `type/enabler` to technical prerequisite issues that unblock stories (e.g. `[Enabler] Configure HttpClient`).
- Apply `type/test` to issues whose primary deliverable is test coverage (e.g. `[Test] Unit tests for GitHubService`).
- Apply `type/bug` to issues representing unexpected or broken behaviour.
- Apply `type/chore` to maintenance tasks, refactoring, or dependency updates with no user-facing change.
- Apply `type/documentation` to issues or PRs that only touch documentation.

#### `priority/` - Always required on issues and PRs
- Apply `priority/critical` only when the issue is blocking all progress or affects production.
- Apply `priority/high` when the issue should be resolved in the current release.
- Apply `priority/medium` as the default for new feature requests.
- Apply `priority/low` for nice-to-have improvements or minor chores.

#### `status/` - Updated as work progresses
- Apply `status/todo` when an issue is ready to start (refined, has acceptance criteria).
- Change to `status/in-progress` when work begins.
- Change to `status/blocked` if the issue cannot proceed because of an external dependency.
- Change to `status/ice-box` if the issue is shelved for later and should leave the active queue (not the same as blocked - no external blocker).
- Change to `status/in-review` when a PR is opened for the issue.
- Change to `status/done` when the issue is closed.

#### `area/` - Apply when the scope is clear
- Apply the relevant `area/` label to all issues and PRs.
- Multiple `area/` labels may be applied if the issue spans more than one feature.
- Apply `area/dashboard` to the Audit Dashboard (`/audit-dashboard`) only.
- Apply `area/actions-templates` to the Actions Templates page (`/actions-templates`).
- Apply `area/planning` to Planning surfaces (Daily Focus, Backlog Review, Iteration, and Repo Management under `/planning`).
- Apply `area/repositories` to the Repositories catalogue page (`/repositories`), including search and refresh, catalogue status, OSS classification and filters ([#440](https://github.com/markheydon/solo-dev-board/issues/440)), repository groups ([#381](https://github.com/markheydon/solo-dev-board/issues/381)), catalogue management actions ([#435](https://github.com/markheydon/solo-dev-board/issues/435)), and overnight OSS hygiene views that scan the catalogue ([#438](https://github.com/markheydon/solo-dev-board/issues/438), [#439](https://github.com/markheydon/solo-dev-board/issues/439)). Do not use `area/dashboard` as a stand-in when the primary deliverable is the Repositories catalogue.
- `area/*` labels on this repository describe SoloDevBoard's own feature map for issue and PR triage. They are **not** part of Label Manager's cross-repository recommended catalogue ([#446](https://github.com/markheydon/solo-dev-board/issues/446)); create or update `area/*` labels on `solo-dev-board` manually when the taxonomy changes.

#### `size/` - Apply during sprint planning
- Size labels are added during planning. They are not required when an issue is first created.
- If an issue is estimated as `size/xl`, consider splitting it into smaller issues before starting.
