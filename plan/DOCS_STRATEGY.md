# SoloDevBoard — Documentation Strategy

<!-- AI Collaborator Instructions: See the "AI Collaborator Instructions" section at the bottom of this file for guidance on documentation responsibilities. -->

This document defines the conventions and processes for maintaining SoloDevBoard's documentation.

---

## Documentation layers

| Layer | Location | Audience |
|-------|----------|----------|
| **Public product site** | `website/` (Hugo / Hextra, GitHub Pages) | Visitors and app users (landing + User Guide) |
| **Developer / operator docs** | `docs/` | Contributors, self-hosters, and operators |
| **Maintainer / PM** | `plan/` | Scope, decisions, runbooks, wireframes |
| **Constitution** | `AGENTS.md`, `CONVENTIONS.md` | AI agents and contributors (always-on rules) |
| **ADRs** | `adr/` | Architectural decision records |

Record new decisions with the `create-architectural-decision-record` skill under `adr/`.

---

## Documentation Structure

```
CHANGELOG.md                     # Keep a Changelog (root)

website/                         # Published public product site (Hugo + Hextra)
├── hugo.yaml
├── go.mod / go.sum
└── content/
    ├── _index.md               # Product landing
    ├── about/                  # Narrative (origin, how we work)
    └── docs/
        ├── _index.md           # User guide landing
        └── <feature>.md        # Per-feature end-user guides

docs/                           # Repository / developer / operator docs (not Pages)
├── README.md                   # Index for developer/operator guides
├── getting-started.md
├── deployment.md
├── hosted-authentication.md
├── github-app.md
├── pat-connectivity.md
├── azure-costs.md
└── observability.md

plan/
├── SCOPE.md
├── DECISIONS.md                # Retired pointer to adr/
├── IMPLEMENTATION_PLAN.md
├── BACKLOG.md
├── RELEASE_PLAN.md
├── LABEL_STRATEGY.md
├── PULL_REQUEST_POLICY.md      # Canonical PR title, body, labels, metadata
├── PROJECT_MANAGEMENT.md
├── PROJECT_BOARD_DESIGN.md
├── PROJECT_README.md           # Canonical Project #8 info pane README
├── SPEC_KIT_MIGRATION.md       # Parked — future Spec Kit adoption
└── DOCS_STRATEGY.md            # This file

adr/
├── README.md                   # ADR conventions
└── adr-NNNN-*.md               # Decision records

scripts/
├── invoke-hugo-site.sh         # Local Hugo build/serve/preview via Docker or Podman (bash)
└── Invoke-HugoSite.ps1         # Windows PowerShell equivalent
```

End-user guides and the product landing are deployed by GitHub Actions (Hugo) to GitHub Pages on `v*` release tags only. Pull requests validate Hugo builds via `hugo-validate.yml` without publishing. See [DEC-019](adr/adr-0019-hugo-hextra-end-user-docs.md), [DEC-021](adr/adr-0021-two-tier-cd-pipeline.md), and [DEC-023](adr/adr-0023-public-product-site-ia-and-domain.md).

---

## Conventions for Documenting New Features

When a new feature is implemented or reaches a stable state:

1. **User Guide Stub → Full Doc:** Update the stub in `website/content/docs/<feature>.md`. Remove the "Under Development" notice. Write the **Overview**, **How to Use**, and **Configuration** sections with accurate, tested information.
2. **Site pages:** Update `website/content/_index.md` and `website/content/docs/_index.md` so the feature appears in the feature list and guide index.
3. **Getting Started / Deployment:** If the feature introduces new configuration (environment variables, AppHost parameters), update `docs/getting-started.md` and `docs/deployment.md`.
4. **Decisions:** If an architectural decision was made, add or update an ADR under `adr/` and update `CONVENTIONS.md` or `AGENTS.md` when rules are cross-cutting.
5. **Issues:** Close or update the corresponding GitHub Issue and sync Project #8.
6. **Local preview:** Run `./scripts/invoke-hugo-site.sh serve` (or `build`) before merging `website/` changes. On Windows, use `.\scripts\Invoke-HugoSite.ps1`.

---

## Screenshot convention

Published end-user guides may include screenshots under:

```
website/static/images/<feature-slug>/<descriptive-name>.png
```

Referenced in Markdown as:

```markdown
![Audit Dashboard overview](/images/audit-dashboard/overview.png)
```

Rules:

- Capture with **light theme** at **1400×900** viewport.
- Use kebab-case PNG filenames and meaningful alt text.
- Always capture with **docs capture mode** enabled (`DocsCapture:Enabled=true`) so only public repositories and public Projects v2 boards appear. See [DEC-020](adr/adr-0020-public-only-docs-capture-mode.md) and [Docs capture mode](../docs/getting-started.md#docs-capture-mode).
- Prefer the Playwright helper: `DOCS_CAPTURE_ENABLED=1 dotnet test tests/E2E/SoloDevBoard.E2E.Tests --filter "Category=DocsCapture"` against a locally running app with a real PAT.
- Do not commit screenshots that show private repositories, private project boards, or other non-public GitHub content.

### Screenshot composition

Prefer screenshots that show a feature **in a useful, populated state**, not just the initial empty shell:

- Where a page offers a **Load** (or equivalent read-only fetch) action after repository selection, capture **after** selecting the example repository and loading data — not the pre-selection empty state.
- Use **`markheydon/solo-dev-board`** as the canonical example repository wherever a repository name is required. This public repository is always available under docs capture mode and keeps screenshots consistent.
- For Planning and Board Rules captures that need a Projects v2 board, prefer the public **SoloDevBoard Roadmap** board linked to that repository rather than other public personal boards.
- Favour **read-only** interactions: load audit summaries, browse labels, inspect board rules, start a triage session (without applying labels or closing issues), or select a workflow template. Do **not** apply migrations, synchronise labels, apply taxonomy, close issues, or write workflow files for documentation screenshots.
- Static pages (Home, About, Appearance) may remain as opening-state captures when no repository-scoped load action exists.
- The Repositories page loads its grid automatically; capture it with the repository list populated (optionally filtered to the example repository).
- **Collapse the app navigation drawer** before capture so feature content fills the viewport. Expanded nav screenshots obscure the feature in landing cards and user-guide hero images; refresh captures with the drawer collapsed when updating `website/static/images/` or landing tiles.

The Playwright docs-capture suite in `tests/E2E/docs-capture/` encodes these composition rules. Extend it when adding new feature guides rather than capturing ad hoc empty states.

---

## Changelog Conventions

SoloDevBoard follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/) conventions. The living changelog is [`CHANGELOG.md`](../CHANGELOG.md) at the repository root. On each tagged release, move the matching `[Unreleased]` notes into a dated `## [X.Y.Z]` section.

Changelog entries are grouped by:
- `Added` — new features.
- `Changed` — changes to existing features.
- `Deprecated` — features to be removed in a future release.
- `Removed` — features that have been removed.
- `Fixed` — bug fixes.
- `Security` — security-related changes.

---

## Cross-linking decisions and planning docs

- Active decisions live in [`adr/`](../adr/).
- Cross-cutting rules live in [`AGENTS.md`](../AGENTS.md) and [`CONVENTIONS.md`](../CONVENTIONS.md).
- Feature and plan docs should link to the relevant `adr/adr-NNNN-*.md` file rather than retired DEC-NNN anchors.
- When a decision is superseded, update the ADR **Status** and add `superseded_by` in the ADR body where appropriate.

---

## Doc Writer Agent Instructions

When Copilot or another AI agent is asked to write or update documentation:

1. **UK English required.** All documentation must use UK English spelling. Run a spell check if possible.
2. **Accuracy over completeness.** Do not document features that are not yet implemented. Use "Coming Soon" or "Under Development" notices for stubs.
3. **Sync with code, E2E, and screenshots.** When updating docs, verify that the documented behaviour matches the current implementation and that Playwright tests in `tests/E2E/SoloDevBoard.E2E.Tests/` assert the same routes, controls, and workflows. Maintain the mapping in [tests/E2E/USER_DOCS_ALIGNMENT.md](../tests/E2E/USER_DOCS_ALIGNMENT.md). If the in-app UI for a published guide or landing tile changed materially, recapture `website/static/images/` in the same docs-update via `DOCS_CAPTURE_ENABLED=1 dotnet test tests/E2E/SoloDevBoard.E2E.Tests --filter "Category=DocsCapture"` (or a focused test filter) against a local app with a real PAT and `DocsCapture:Enabled=true`. Update `DocsCapture/` helpers when the prepare path no longer shows the documented state. Do not leave recapture as a later note. If capture cannot run, report it as blocked.
4. **Link generously.** Cross-reference related docs, decisions, and planning files. Use relative links within the same docs tree; use GitHub blob URLs when linking from the published site to repository-only files.
5. **Heading hierarchy.** Use H1 for the page title, H2 for major sections, H3 for subsections. Do not skip levels.
6. **Code blocks.** All code, commands, and configuration snippets must be in fenced code blocks with the appropriate language identifier.
7. **Tables for structured data.** Prefer Markdown tables over bullet lists for structured comparisons (e.g. configuration options, label taxonomy).
8. **Update site indexes last.** After writing a user guide page, check whether `website/content/_index.md` and `website/content/docs/_index.md` need updates.
9. **Audience split.** Keep operator/self-hoster material in `docs/`; keep in-app end-user material in `website/content/docs/`.

---

## AI Collaborator Instructions

> When code changes are made, the following documentation updates are required:
>
> | Code Change | Documentation Action |
> |-------------|---------------------|
> | New end-user feature implemented | Update `website/content/docs/<feature>.md` to full content; remove "Under Development" notice; refresh site indexes; recapture feature screenshots when the UI changed |
> | New environment variable | Update `docs/getting-started.md` configuration table and `docs/deployment.md` |
> | New architectural decision | Add an ADR under `adr/`; update `CONVENTIONS.md` or `AGENTS.md` if cross-cutting |
> | Scope change | Update `plan/SCOPE.md`; update `website/content/_index.md` if the published feature list changes |
> | New release | Update `plan/RELEASE_PLAN.md` and [`CHANGELOG.md`](../CHANGELOG.md) |
> | New label | Update `plan/LABEL_STRATEGY.md` |
> | New board column or rule | Update `plan/PROJECT_BOARD_DESIGN.md` |
> | Project #8 info pane / phase status | Update `plan/PROJECT_README.md` and paste into the project info pane |
>
> Documentation changes should be included in the **same PR** as the code change where possible. A PR that adds a feature without updating the relevant docs is considered incomplete.
