# v1.1 hardcoding audit — maintainer identity and repo assumptions

This document records the shipped-app and user-guide audit for issue [#423](https://github.com/markheydon/solo-dev-board/issues/423) (parent feature [#272](https://github.com/markheydon/solo-dev-board/issues/272)).

## Scope

| Included | Excluded (acceptable by definition) |
|----------|-------------------------------------|
| `src/SoloDevBoard.App/`, `src/Application/`, `src/Infrastructure/`, `src/SoloDevBoard.AppHost/` | Non-shipped repo meta: `.agents/`, `plan/` (except this inventory), `.github/workflows/roadmap-sync.yml` |
| `website/content/docs/` (published User Guide) | Operator docs under `docs/` |
| Product-behaviour fixtures under `tests/` that imply shipped defaults | Docs-capture helpers and clearly labelled test login fixtures |
| Example strings: `markheydon`, `solo-dev-board`, `Project #8`, Roadmap Sync identifiers | Marketing About / landing origin narrative (`website/content/about/`, landing `_index.md`) as project story, not app instructions |

Related decisions: [DEC-020](DECISIONS.md#dec-020-public-only-docs-capture-mode-for-documentation-screenshots), [DEC-027](DECISIONS.md#dec-027-post-10-milestone-and-work-item-hierarchy), [DEC-029](DECISIONS.md#dec-029-cross-repo-planning-board-selection-and-local-settings).

## Classification rules

**Acceptable**

- Open-source attribution to `https://github.com/markheydon/solo-dev-board`.
- Docs-capture example repository when gated by `DocsCapture:Enabled` (DEC-020).
- Product brand prefixes such as `solo-dev-board.*` claim types and localStorage keys.
- **Opinionated product contracts** for Cross-Repo Planning and Label Manager: SoloDevBoard label taxonomy (`type/`, `priority/`, `status/` and related names), Projects v2 Status display names matched by name (`Up Next`, `In Progress`, `Blocked`, `Ice Box`, and review-equivalent names), and the optional `Focus Order` field name. These are deliberate product behaviour (see [`LABEL_STRATEGY.md`](LABEL_STRATEGY.md) and the built-in SoloDevBoard recommended taxonomy), not maintainer dogfood leaks. Boards and repositories remain **user-selected** (DEC-029); Project #8 option ids are not compiled into `src/`.

**Must fix**

- Default owner, repository, organisation, or Projects v2 board identifiers baked into startup or PM UI without user configuration.
- User Guide copy that treats Project #8, Roadmap Sync, `.agents/`, or maintainer-only companion repos as product setup steps.
- Application or Infrastructure paths that assume the authenticated user is the upstream maintainer.
- User Guide steps that only work for `markheydon/solo-dev-board` unless labelled as an optional upstream / docs-capture example.

## Method

1. Ripgrep `src/`, `website/content/docs/`, and `tests/` for `markheydon`, `solo-dev-board`, `Project #8`, Roadmap Sync, and related identifiers.
2. Review each hit in context against the rules above.
3. Confirm DocsCapture defaults to disabled and filters only when enabled.
4. Confirm no compiled Project #8 Status option ids appear under `src/`.

## Inventory by layer

### `src/SoloDevBoard.App/`

| Finding | Classification | Notes |
|---------|----------------|-------|
| About page `RepositoryAddress` → `https://github.com/markheydon/solo-dev-board` | Acceptable | OSS attribution. |
| Claim types / cookie / localStorage keys prefixed `solo-dev-board.` | Acceptable | Product brand namespace, not a GitHub owner. |
| Planning UI copy referencing Up Next / In Progress / capacity | Acceptable | Opinionated Status-name contract; board is user-selected. |
| Placeholders (`owner/name`, `owner/repo`) | Acceptable | Generic. |
| No default repository or project node id in startup or PM settings | Acceptable | `PlanningSettingsDefaults` leaves `PlanningBoardNodeId` null. |

### `src/Application/`

| Finding | Classification | Notes |
|---------|----------------|-------|
| No `markheydon` / `solo-dev-board` / Project #8 string hits | Acceptable | — |
| `PlanningLabelHelpers`, `PlanningPriorityRanker`, SoloDevBoard recommended taxonomy catalogue | Acceptable | Deliberate opinionated labelling strategy. |
| `DailyFocusBoardStateMapper` / recommendation Status names; GraphQL consumers of `Focus Order` | Acceptable | Deliberate PM board field conventions matched by display name (DEC-029). |
| `PlanningSettingsDefaults` capacity / stall / neglect numbers | Acceptable | Product defaults, not maintainer identity. |

### `src/Infrastructure/`

| Finding | Classification | Notes |
|---------|----------------|-------|
| `DocsCaptureOptions.Enabled` defaults to `false` | Acceptable | DEC-020; local screenshot hygiene only. |
| DocsCapture filters public repositories / public project boards when enabled | Acceptable | Not a hosted product default; not tied to a specific owner login. |
| Hosted claim type constants `solo-dev-board.github.*` | Acceptable | Brand namespace. |
| Project item catalogue GraphQL uses field names `Status` and `Focus Order` | Acceptable | Opinionated field-name contract; discovers field ids at runtime. |
| No hardcoded Project #8 option ids | Acceptable | Confirmed by search. |

### `src/SoloDevBoard.AppHost/`

| Finding | Classification | Notes |
|---------|----------------|-------|
| No maintainer login or repository slug in AppHost parameters / appsettings | Acceptable | Admission allow-lists default to `-` / empty until operator-supplied. |
| Resource Azure names (`app`, `aca`, …) | Acceptable | Hosting names, not GitHub identity. |

### `src/Domain/`

| Finding | Classification | Notes |
|---------|----------------|-------|
| `Label.RepositoryName` XML example previously used `solo-dev-board` | Acceptable → cleaned | Illustrative only; example generalised to `example-repo` in this delivery. |

### `website/content/docs/`

| Finding | Classification | Notes |
|---------|----------------|-------|
| Screenshot alt text naming `markheydon/solo-dev-board` | Acceptable | DEC-020 docs-capture example repository shown in published screenshots. |
| Links to upstream `docs/`, `plan/`, and issue trackers under `github.com/markheydon/solo-dev-board` | Acceptable | OSS / contributor deep links, not in-app defaults. |
| `planning.md` overview linked `markheydon/github-workflows` as if adopters needed that companion | **Must fix** | Remediated in this delivery — generic product overview; opinionated conventions called out explicitly. |
| Label Manager / Audit copy referring to SoloDevBoard taxonomy | Acceptable | Product feature. |
| PM guide documenting Up Next / Focus Order / `type/`/`priority/` behaviour | Acceptable | Documents the opinionated contract. |

### `tests/`

| Finding | Classification | Notes |
|---------|----------------|-------|
| Widespread `markheydon` / `solo-dev-board` fixtures in unit and component tests | Acceptable | Test data only; does not ship as product defaults. |
| `tests/E2E/docs-capture/` example repository constant | Acceptable | DEC-020; docs-capture only. |
| E2E journey specs assert routes and UI shells, not a fixed owner default | Acceptable | — |

### Out of User Guide scope (noted)

| Finding | Classification | Notes |
|---------|----------------|-------|
| `website/content/about/origin.md`, landing lineage to `github-workflows` | Acceptable | Project origin narrative on the marketing site. |
| `website/content/about/how-we-work.md` mentions Project #8 and `.agents/` | Acceptable | Describes how **this** open-source project is run, not SoloDevBoard product setup. |

## Must-fix remediation

| Item | Action | Status |
|------|--------|--------|
| User Guide PM overview referenced `markheydon/github-workflows` | Rewrite overview; add **Opinionated conventions** section | Fixed in this delivery |
| Domain XML example used `solo-dev-board` | Use `example-repo` | Fixed in this delivery |

No further must-fix clusters required a follow-up issue after maintainer confirmation that label taxonomy and related PM workflow conventions are deliberate product opinionation.

## Acceptance criteria mapping

| Criterion | Status |
|-----------|--------|
| App / Application / Infrastructure / AppHost audited | Done — inventory above. |
| Example-string search across `src/` classified | Done. |
| `website/content/docs/` audited | Done; one must-fix remediated. |
| Tests / fixtures reviewed | Done — acceptable. |
| Written inventory with acceptable vs must fix | This document. |
| Must-fix resolved or deferred with issue refs | Resolved in this delivery; none deferred. |
| No new maintainer-specific hardcoding introduced | Confirmed for this change set. |
