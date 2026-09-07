# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.2.0] - 2026-09-07

Planning polish, Reload & Templates release. GitHub milestone [`v1.2 - Planning polish, Reload & Templates`](https://github.com/markheydon/solo-dev-board/milestone/8) closed with 30 issues delivered.

### Added

- **Planning** — optional **Limit Recommended today to the selected planning board** switch on Daily Focus; the default remains all included repositories ([#403](https://github.com/markheydon/solo-dev-board/issues/403)).
- **Reload from GitHub** — page-level reload that keeps repository, board, and in-page selections across Audit Dashboard, Repositories, One-Click Migration, Label Manager, Board Rules Visualiser, Triage UI, Actions Templates, and related catalogue surfaces ([#447](https://github.com/markheydon/solo-dev-board/issues/447), [#449](https://github.com/markheydon/solo-dev-board/issues/449), [#451](https://github.com/markheydon/solo-dev-board/issues/451)).
- **Actions Templates** — load one additional GitHub `owner/name` custom template source via the catalogue selector or a manual field, merge `.github/workflows` YAML with built-ins, and remember the last-used source in browser storage ([#292](https://github.com/markheydon/solo-dev-board/issues/292), [#497](https://github.com/markheydon/solo-dev-board/issues/497)).
- **Triage UI** — disposition-based **Save and next**, **Close as duplicate and next**, and **Skip and next** primary actions with keyboard shortcuts ([#492](https://github.com/markheydon/solo-dev-board/issues/492)).

### Changed

- **Triage UI** — milestone and project board fields commit with the quick label in one **Save and next** action instead of separate primary buttons ([#492](https://github.com/markheydon/solo-dev-board/issues/492)).
- **Hosted authentication** — separate named `HttpClient` instances for GitHub OAuth token exchange and GitHub REST API calls so REST requests use the correct `Accept` and `User-Agent` defaults ([#453](https://github.com/markheydon/solo-dev-board/issues/453)).

### Fixed

- **Triage UI** — closing an item as duplicate no longer removes labels that were not part of the duplicate disposition ([#488](https://github.com/markheydon/solo-dev-board/issues/488), [#502](https://github.com/markheydon/solo-dev-board/issues/502)).
- **Triage UI** — **Process** no longer replaces the full label set when only a quick label is applied ([#502](https://github.com/markheydon/solo-dev-board/issues/502)).

### Known limitations (not in this release)

- Persisted default workflow template parameter profiles ([#436](https://github.com/markheydon/solo-dev-board/issues/436)) remain planned for a later release.
- Server-backed persistence for the custom Actions template source waits on an Aspire product store ([#498](https://github.com/markheydon/solo-dev-board/issues/498), blocked by [#391](https://github.com/markheydon/solo-dev-board/issues/391)).
- Private user-owned Projects v2 under hosted GitHub App sign-in remain platform-blocked ([#293](https://github.com/markheydon/solo-dev-board/issues/293)).
- Repositories Add / Remove / Bulk / Edit / More actions remain stubs ([#435](https://github.com/markheydon/solo-dev-board/issues/435)).
- Full GitHub Project v2 automation-rule retrieval remains a later Board Rules slice ([#437](https://github.com/markheydon/solo-dev-board/issues/437)).
- Window-focus refetch after Reload from GitHub remains ice-boxed ([#450](https://github.com/markheydon/solo-dev-board/issues/450)).

## [1.1.0] - 2026-08-31

Cross-Repo Planning & Refinement release. GitHub milestone [`v1.1 - Cross-Repo Planning & Refinement`](https://github.com/markheydon/solo-dev-board/milestone/7) closed with 91 issues delivered.

### Added

- **Planning** — Daily Focus, Backlog Review, Iteration Planning, Repo Management, and conditional Board setup ([#272](https://github.com/markheydon/solo-dev-board/issues/272)–[#288](https://github.com/markheydon/solo-dev-board/issues/288)).
- **Audit Dashboard** label consistency warnings against the SoloDevBoard taxonomy ([#290](https://github.com/markheydon/solo-dev-board/issues/290)).
- **One-Click Migration** of Projects v2 Status columns, including board selectors and conflict strategies ([#291](https://github.com/markheydon/solo-dev-board/issues/291)).
- **Repositories** catalogue classification from the GitHub `open-source` topic, with **All** / **Open source** / **Not open source** filters ([#440](https://github.com/markheydon/solo-dev-board/issues/440)).
- Label Manager bulk delete on the Labels tab ([#444](https://github.com/markheydon/solo-dev-board/issues/444)).

### Changed

- Label Manager recommended catalogue omits this repository's `area/*` map; nested **Keep `area/*` labels** (default on) during extra cleanup ([#446](https://github.com/markheydon/solo-dev-board/issues/446)).
- One-Click Migration label **Overwrite** keeps target `area/*` labels by default, matching Label Manager ([#464](https://github.com/markheydon/solo-dev-board/issues/464)).
- Iteration Planning: stall is the only hard disable for Add to Up Next; capacity remains a meter plus confirm ([#445](https://github.com/markheydon/solo-dev-board/issues/445)).
- Transient outcomes use snackbars; persistent load errors sit at the top of the affected section ([#465](https://github.com/markheydon/solo-dev-board/issues/465), [#473](https://github.com/markheydon/solo-dev-board/issues/473)).

### Known limitations (not in this release)

- Custom workflow template repositories ([#292](https://github.com/markheydon/solo-dev-board/issues/292)) remain ice-boxed.
- Private user-owned Projects v2 under hosted GitHub App sign-in remain platform-blocked ([#293](https://github.com/markheydon/solo-dev-board/issues/293)).
- Repositories Add / Remove / Bulk / Edit / More actions remain stubs ([#435](https://github.com/markheydon/solo-dev-board/issues/435)).
- Full GitHub Project v2 automation-rule retrieval remains a later Board Rules slice ([#437](https://github.com/markheydon/solo-dev-board/issues/437)).

## [1.0.0] - 2026-08-18

First public production release: six core tools (Audit Dashboard, Label Manager, One-Click Migration, Board Rules Visualiser, Triage UI, Actions Templates), hosted GitHub App authentication with admission control, PAT-only local trusted mode, Aspire deployment to Azure Container Apps, and the Hugo product site at [solodevboard.com](https://solodevboard.com/).

See the [v1.0.0 GitHub Release](https://github.com/markheydon/solo-dev-board/releases/tag/v1.0.0).
