# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

When you tag `v1.1.0`, rename the **Unreleased** heading below to `## [1.1.0] - YYYY-MM-DD` and leave a fresh empty Unreleased section for the next increment.

## [Unreleased] — forthcoming v1.1.0

GitHub milestone [`v1.1 - Cross-Repo Planning & Refinement`](https://github.com/markheydon/solo-dev-board/milestone/7) is complete (90 closed, 0 open). The public site and production app update when the `v1.1.0` tag is pushed.

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
