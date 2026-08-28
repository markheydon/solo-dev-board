# SoloDevBoard — Project Scope

<!-- AI Collaborator Instructions: When scope changes, update this file and create/close GitHub issues accordingly. Add a new entry to the changelog at the bottom of this file noting the date and nature of the scope change. -->

## Project Vision

SoloDevBoard is a **single pane of glass** for solo developers who maintain multiple GitHub repositories. The goal is to eliminate context-switching and reduce the friction of routine GitHub housekeeping tasks — triaging issues, managing labels, auditing project health, and deploying workflow configurations — by surfacing everything in one cohesive, AI-friendly application.

> **Motivating context:** SoloDevBoard was directly inspired by the AI-driven PM workflow built in the companion repository [markheydon/github-workflows](https://github.com/markheydon/github-workflows). That system uses VS Code Copilot agents and prompts to manage cross-repo workloads across two operating modes — a weekly **PM Mode** (scan all repos, triage issues and PRs, curate the project board) and a daily **Work Mode** (pick the next item from a pre-curated board). SoloDevBoard's long-term destination is to provide a proper visual interface for everything that system does today via text prompts and scripts: daily focus views, cross-repo backlog prioritisation, iteration planning, label strategy enforcement, workflow migration, and issue triage. Each intermediate phase delivers individual tooling features; Epic 7 (Cross-Repo Planning, Phase 5) closes the loop by bringing the planning intelligence into the UI.

---

## In Scope

The following features define the current scope of SoloDevBoard:

### Production-Readiness Infrastructure and Authentication (v1.0.0)

Public-release hardening is in scope for v1.0.0, including:
- GitHub App-first hosted authentication and installation token flow for user-to-server authentication.
- Hosted access control and explicit authorised-user admission for public deployments. Only users or organisations explicitly authorised by the operator may access the hosted UI.
- Secure production authentication handling for hosted deployments via Aspire AppHost parameters supplied at deploy time (GitHub Environment secrets).
- Azure deployment via Aspire to Azure Container Apps with scale-to-zero ([DEC-015](DECISIONS.md#dec-015-aspire-azure-container-apps-deployment)).
- OIDC authentication for GitHub Actions to Azure.
- Operational hardening: response caching, health checks, structured logging, Application Insights telemetry, Dependabot configuration.
- PAT-only local trusted mode for development and trusted personal self-hosted use (see [docs/getting-started.md](../docs/getting-started.md#pat-only-local-trusted-mode) and [self-hoster PAT deploy](../docs/deployment.md#self-hoster-deployment-pat-mode)).
- Separate OAuth App registration is not the intended default end state; it is only a fallback if GitHub App user authentication proves insufficient for hosted sign-in requirements.
- Hosted authentication is secure-by-default, with deny-by-default admission control.

### 1. Audit Dashboard

A consolidated view of repository health: open issues, stale PRs, GitHub Actions workflow statuses, and label consistency warnings ([#290](https://github.com/markheydon/solo-dev-board/issues/290)) across selected repositories.

### 2. One-Click Migration

Copy labels and milestones from a source repository to one or more target repositories in a single, preview-first action. Project board column migration shipped on milestone **`v1.1`** ([#291](https://github.com/markheydon/solo-dev-board/issues/291)). Label **Overwrite** follows the Label Manager keep-`area/*` rule (default on) so target area labels are not deleted when the source catalogue omits them ([#464](https://github.com/markheydon/solo-dev-board/issues/464), [DEC-036](DECISIONS.md#dec-036-one-click-migration-label-overwrite-keeps-area-by-default)).

### 3. Label Manager

Create, edit, recolour, delete, and synchronise GitHub labels across multiple repositories from a single interface, using a canonical label taxonomy as the source of truth. **`v1.1` refinements:** bulk delete on the Labels tab ([#444](https://github.com/markheydon/solo-dev-board/issues/444)) and omit this repository's `area/*` labels from the built-in catalogue, with a nested keep option during extra cleanup ([#446](https://github.com/markheydon/solo-dev-board/issues/446), [DEC-034](DECISIONS.md#dec-034-label-manager-recommended-catalogue-omits-area-labels)).

### 4. Board Rules Visualiser

An interactive diagram showing the automation rules configured on GitHub project boards, making it easy to understand how issues flow between columns.

### 5. Triage UI

A focused, keyboard-friendly interface for triaging incoming GitHub issues one at a time, supporting quick labelling, milestone assignment, and duplicate closure.

### 6. Actions Templates

Browse, customise, and apply built-in GitHub Actions workflow templates across repositories, with tracking of which repositories have which templates applied. Custom template repositories are **deferred past `v1.1`** ([#292](https://github.com/markheydon/solo-dev-board/issues/292), ice-boxed).

### 7. Planning

A UI-based implementation of the two-mode PM operating system from [markheydon/github-workflows](https://github.com/markheydon/github-workflows): a Daily Focus view (board state, stalled items, top priorities), a cross-repository Backlog Review (prioritised work across all repos, neglected repo detection), and an Iteration Planning tool (capacity management, Up Next curation, milestone assignment). Tracked in [#272](https://github.com/markheydon/solo-dev-board/issues/272) on milestone **`v1.1`** ([DEC-027](DECISIONS.md#dec-027-post-10-milestone-and-work-item-hierarchy)). **`v1.1` refinement:** keep the stall gate as the only hard disable for Add to Up Next, and keep capacity as a meter plus confirm ([#445](https://github.com/markheydon/solo-dev-board/issues/445)).

### 8. Public product site (GitHub Pages)

The Hugo/Hextra site in `website/` (published at `https://solodevboard.com/`) serves as the public product front door: marketing landing (product and project), narrative About pages, and User Guide at `/docs/`. Developer and operator documentation remains in `docs/` and is not published on the product domain (DEC-019, DEC-023).

### 9. Repositories catalogue and OSS identification

The Repositories page lists GitHub repositories available to the authenticated account (search, refresh, visibility and status chips). **`v1.1`** adds first-class open-source identification from the GitHub topic `open-source`, with built-in **Open source** and **Not open source** filters on that page ([#440](https://github.com/markheydon/solo-dev-board/issues/440), [DEC-032](DECISIONS.md#dec-032-oss-catalogue-identification-from-the-github-open-source-topic)). Repository groups ([#381](https://github.com/markheydon/solo-dev-board/issues/381)) and overnight OSS hygiene views ([#438](https://github.com/markheydon/solo-dev-board/issues/438), [#439](https://github.com/markheydon/solo-dev-board/issues/439)) consume that classification later; they are not prerequisites.

---

## Out of Scope

The following are explicitly **not** in scope for the current version of SoloDevBoard:

- Collaboration and team features — SoloDevBoard is designed for a single developer. No shared sessions, team boards, or collaborative user management are in scope.
- Non-GitHub providers — GitLab, Bitbucket, Azure DevOps, and other platforms are not supported. GitHub.com is the only supported provider initially.
- Mobile application — SoloDevBoard is a web application. No native iOS or Android app is planned.
- Real-time collaboration — No shared sessions, shared boards, or live collaboration features.
- Issue content editing — SoloDevBoard manages metadata (labels, milestones, assignments) but does not provide a full issue editor.
- GitHub Sponsors, billing-backed entitlement automation, and marketplace monetisation are not in scope for v1.0.0. No integration with GitHub Marketplace, billing APIs, or paid access flows is planned for this release.

## Assumptions

- **Single-user / solo developer:** The application is used by one person who is the owner or an admin of all managed repositories.
- **GitHub.com initially:** The application targets GitHub.com. GitHub Enterprise Server support may be considered in a future release.
- **.NET 10:** The application is built on .NET 10 and Blazor Server. No legacy .NET Framework support is required.
- **Modern browser:** Users are assumed to be using a current version of Chrome, Firefox, Edge, or Safari.
- **Internet connection:** The application requires a live connection to the GitHub API.

---

## Constraints

- **UK English:** All user-facing text, code comments, documentation, and commit messages must be written in UK English.
- **Open source:** The project is intended to be open source under the MIT Licence.
- **AI-driven development:** The project is developed with GitHub Copilot as an active collaborator. All planning documents are written to be machine-readable and actionable by AI agents.
- **Minimal dependencies:** Prefer the .NET ecosystem and well-established open source libraries. Avoid adding dependencies without a decision log entry ([`plan/DECISIONS.md`](DECISIONS.md)).
- **UI component library:** MudBlazor is the sole UI component library for the Blazor front-end (see [DEC-009](DECISIONS.md#dec-009-mudblazor-as-the-sole-ui-component-library)). Raw HTML form elements are not used where a MudBlazor equivalent exists.

---

## AI Collaborator Instructions

> When this project's scope changes — whether a feature is added, removed, or modified — follow these steps:
>
> 1. Update the **In Scope** or **Out of Scope** sections of this file to reflect the change.
> 2. Update `plan/IMPLEMENTATION_PLAN.md` if the phase breakdown is affected.
> 3. Create or update GitHub Issues for the scope change and sync Project #8.
> 4. Create a GitHub Issue for the scope change using the `type/feature` or `type/chore` template.
> 5. If the change affects the architecture, follow `repo-decision-log` — update [`plan/DECISIONS.md`](DECISIONS.md) and/or constitution.
> 6. If the change affects the user-facing docs, update or create the relevant file in `website/content/docs/`.
> 7. Add a changelog entry at the bottom of this file.

---

## Scope Changelog
| Date | Change | Author |
|------|--------|--------|
| 2025-01-01 | Initial scope defined | Solo developer |
| 2026-03-06 | Multi-user / team features updated from permanently out of scope to deferred (Phase 5). `ICurrentUserContext` interface preparation added to Phase 2. See ADR-0007. | Solo developer |
| 2026-03-07 | Added Epic 7 (Cross-Repo Planning) to In Scope. Updated Project Vision to document the motivating context from markheydon/github-workflows. Phase 5 added to IMPLEMENTATION_PLAN.md for this epic. | Solo developer |
| 2026-03-09 | Added constraint: MudBlazor is the sole UI component library (ADR-0012). Fluent UI Blazor library removed. Existing UI features (Repositories page, Label Manager) to be refactored. | Solo developer |
| 2026-03-12 | Public-release hardening and public authentication planning brought into scope for v1.0.0. Production authentication, Azure infrastructure, operational hardening, and dependency hygiene are now in scope for the upcoming public release milestone. | Solo developer |
| 2026-03-13 | Clarified hosted access control for public deployments. Admission control for hosted environments is now explicitly in scope for v1.0.0. GitHub Sponsors, billing-backed entitlement automation, and marketplace monetisation remain out of scope for this release. | Solo developer |
| 2026-03-13 | Refined hosted authentication direction to GitHub App-first, ideally GitHub App-only, with OAuth App dependency demoted to fallback status. PAT-only local trusted mode and secure-by-default hosted admission control preserved. (ADR-0015) | Solo developer |
| 2026-07-18 | Backlog-to-issues migration complete: GitHub Issues are the single source of truth for work items. `plan/BACKLOG.md` retired as a living queue (slim index only). Open work migrated to Issues #247–#259 (v1.0.0), #272–#288 (Phase 5), and #289–#293 (post-v1). | Solo developer |
| 2026-07-18 | Planning sync: Phases 1–4 marked complete in BACKLOG.md and IMPLEMENTATION_PLAN.md. Deferred follow-on slices (label consistency warnings, project board migration, custom workflow template repos, Audit Dashboard enhancements) explicitly tracked. Phase 5 and Phase 6 remain the only active delivery phases. | Solo developer |
| 2026-07-25 | V1 auth polish bundle clarified: hosted sign-in entry UX (#249) and PAT-mode GitHub connectivity readiness (#314) split from operator documentation (#247, #248). In-app secret editing remains out of scope; operator configuration stays via Aspire parameters, user secrets, and Key Vault. | Solo developer |
| 2026-07-25 | PAT-only local trusted mode and self-hoster PAT Azure deploy path formalised in docs (#247, #248; PR #324). Scope bullet now links to getting-started and deployment guides. | Solo developer |
| 2026-08-16 | Public product site on GitHub Pages: landing, User Guide, About narrative, canonical `solodevboard.com`, source tree `website/` (DEC-023). | Solo developer |
| 2026-08-20 | Label consistency warnings delivered on the Audit Dashboard ([#290](https://github.com/markheydon/solo-dev-board/issues/290)). | Solo developer |
| 2026-08-23 | Repositories catalogue OSS identification in scope for v1.1.0: GitHub topic `open-source` plus built-in Open source / Not open source filters ([#440](https://github.com/markheydon/solo-dev-board/issues/440), DEC-032). Groups (#381) remain a later increment. | Solo developer |
| 2026-08-23 | Remaining v1.1.0 dogfood: Label Manager bulk delete (#444), omit `area/*` from the built-in catalogue (#446, DEC-034), Iteration Planning stall vs capacity copy (#445). Unmilestoned backlog stays off this milestone. | Solo developer |
| 2026-08-26 | One-Click Migration label Overwrite keeps `area/*` by default, matching Label Manager extra-delete behaviour (#464, DEC-036). Configurable keep-prefix lists remain out of scope. | Solo developer |
| 2026-08-28 | GitHub milestones renamed to `vX.Y - Descriptive name` (ASCII hyphen; release tags remain SemVer `vX.Y.Z`). Open milestone titled `v1.1 - Cross-Repo Planning & Refinement`. Custom template repositories ([#292](https://github.com/markheydon/solo-dev-board/issues/292)) deferred past this release. Project board column migration ([#291](https://github.com/markheydon/solo-dev-board/issues/291)) marked complete. | Solo developer |

