# SoloDevBoard — Release Plan

**Open work:** [GitHub Issues](https://github.com/markheydon/solo-dev-board/issues). **Which release is open:** [milestones](https://github.com/markheydon/solo-dev-board/milestones). This file covers versioning, the release runbook, and historical release scope.

## Versioning Strategy

SoloDevBoard follows [Semantic Versioning](https://semver.org/) (`MAJOR.MINOR.PATCH`):

- **MAJOR** — Breaking changes to public APIs, configuration schemas, or deployment requirements.
- **MINOR** — New features added in a backwards-compatible manner.
- **PATCH** — Backwards-compatible bug fixes and minor improvements.

During the pre-1.0 development phase (`0.x.y`), minor version bumps may include breaking changes as the application stabilises.

### Build-time versioning (MinVer)

Application version numbers are calculated automatically at build time by [MinVer](https://github.com/adamralph/minver) from git tags on `SoloDevBoard.Web`. The About page and GitHub API user-agent both read this stamped assembly metadata.

| Deploy tier | Git state | Example About version | Example built at | Example build metadata |
|---|---|---|---|---|
| **Production** | Build at tag `v1.0.0` | `1.0.0` | *(not shown)* | Short commit SHA (for example `abc1234`) |
| **Staging** | `main` commits after the latest tag | `1.0.1-staging.0.42` | `23 Aug 26 @ 15:11 BST` | Short commit SHA |
| **Local dev** | Untagged working tree | `1.0.x-staging.0.n` (from git history) | `23 Aug 26 @ 15:11 BST` | Short commit SHA when git metadata is available |

**Production source of truth:** the `vX.Y.Z` git tag pushed for the release. Pushing that tag triggers production CD; MinVer stamps the tag version into the deployed assembly.

**Staging identification:** staging builds use the `staging.0` pre-release identifier so the About version is clearly not a production release. The About **Built** line shows the compile-time stamp in UK local time (for example `23 Aug 26 @ 15:11 BST`) so you can confirm a new deploy without memorising the MinVer suffix. Compare the About **Build** value with the commit SHA on `main` in GitHub to confirm which deployment you are viewing.

**CI requirement:** CD and CI workflows check out the repository with `fetch-depth: 0` so MinVer can read tags and commit history.

---

## Release Roadmap

### v0.1.0 - Foundation / MVP

**Goal:** A working Blazor Server application deployed to Azure that can authenticate with GitHub and display a list of repositories.

**Scope:**
- GitHub PAT authentication
- Repository listing
- Empty dashboard shell (placeholder panels for all 6 features)
- CI pipeline
- Azure Container Apps deployment via Aspire

**Target:** End of Phase 1 (see [archive/IMPLEMENTATION_PLAN.md](archive/IMPLEMENTATION_PLAN.md))

---

### v0.2.0 - Core Features

**Goal:** Deliver the Label Manager and Audit Dashboard features.

**Scope:**
- Label Manager: view, create, edit, delete, and synchronise labels across repositories
- Audit Dashboard: open issues, stale PRs, workflow statuses (label consistency warnings deferred to v1.1.0)

**Target:** End of Phase 2

---

### v0.3.0 - Migration and Triage

**Goal:** Deliver the One-Click Migration and Triage UI features.

**Scope:**
- One-Click Migration: labels and milestones
- Triage UI: keyboard-friendly issue triage with quick actions

**Target:** End of Phase 3

---

### v0.4.0 - Visualisation and Templates

**Goal:** Deliver the Board Rules Visualiser and Actions Templates features.

**Scope:**
- Board Rules Visualiser: interactive diagram of project board automation rules
- Actions Templates: built-in template library, apply to repositories

**Status:** Complete (2026-07-17).

**Target:** End of Phase 4

---

### v1.0.0 - Production Ready

**Goal:** A stable, well-tested, and fully documented release of the six shipped tools, suitable for regular hosted and self-hosted use.

**Scope:**
- GitHub App authentication for hosted deployments (PAT remains for local trusted and self-hoster paths)
- ≥80% unit test coverage on Application and Domain
- Accessibility audit of primary journey shells (WCAG 2.1 AA)
- Performance review and optimisation
- Complete user-facing documentation for shipped features (Hugo site at `https://solodevboard.com/`; Planning guide ships with `v1.1.0`)
- Full Azure deployment pipeline with staging and production environment gates

**Status:** Complete (2026-08-18). Tagged [`v1.0.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.0.0). Production CD and GitHub Pages publish from `v*` tags.

**Sequencing note:** Selected hosted-authentication and Azure-delivery items were pulled forward to support safe hosted validation. The six core tools shipped in v1.0.0. Post-1.0 delivery uses a single active milestone model ([DEC-027](adr/adr-0027-post-1-0-milestone-and-work-item-hierarchy.md)).

---

### v1.1 - Cross-Repo Planning & Refinement

**Goal:** Close out incomplete v1.0 slices, deliver Planning, and ship post-release refinements from public use.

**Scope:**
- Label consistency warnings on the Audit Dashboard ([#290](https://github.com/markheydon/solo-dev-board/issues/290)) — extends [#40](https://github.com/markheydon/solo-dev-board/issues/40) — **complete**
- Project board column migration ([#291](https://github.com/markheydon/solo-dev-board/issues/291)) — extends [#88](https://github.com/markheydon/solo-dev-board/issues/88) — **complete**
- Custom workflow template repositories ([#292](https://github.com/markheydon/solo-dev-board/issues/292)) — **deferred** (ice-boxed; post-`v1.1` backlog)
- Planning ([#272](https://github.com/markheydon/solo-dev-board/issues/272)–[#288](https://github.com/markheydon/solo-dev-board/issues/288)): Daily Focus, Backlog Review, Iteration, Repo Management — **complete**
- OSS catalogue identification ([#440](https://github.com/markheydon/solo-dev-board/issues/440)) — **complete**
- Post-release refinements: Label Manager bulk delete and keep-`area/*` ([#444](https://github.com/markheydon/solo-dev-board/issues/444), [#446](https://github.com/markheydon/solo-dev-board/issues/446)), Iteration stall/capacity UX ([#445](https://github.com/markheydon/solo-dev-board/issues/445)), snackbar standardisation ([#465](https://github.com/markheydon/solo-dev-board/issues/465)), migration keep-`area/*` ([#464](https://github.com/markheydon/solo-dev-board/issues/464), [#471](https://github.com/markheydon/solo-dev-board/issues/471)), feedback region cleanup ([#473](https://github.com/markheydon/solo-dev-board/issues/473)) — **complete**

Platform-blocked and ice-box items stay on GitHub Issues; they are not repeated here.

**Status:** Complete (2026-08-31). Tagged [`v1.1.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.1.0). Production CD and GitHub Pages publish from `v*` tags.

**Target:** Early September 2026 (met)

---

### v1.2 - Planning polish, Reload & Templates

**Goal:** Finish older unfinished slices after `v1.1.0`: Planning follow-through, Reload from GitHub, hosted-auth HttpClient hygiene, custom workflow template repositories, and Triage action-surface usability.

**Scope:**
- Optional board-scoped Daily Focus recommendations ([#403](https://github.com/markheydon/solo-dev-board/issues/403))
- Reload from GitHub, keep selection ([#447](https://github.com/markheydon/solo-dev-board/issues/447), [#449](https://github.com/markheydon/solo-dev-board/issues/449), [#451](https://github.com/markheydon/solo-dev-board/issues/451))
- Split hosted GitHub OAuth and API HttpClients ([#453](https://github.com/markheydon/solo-dev-board/issues/453))
- Custom workflow template repositories ([#292](https://github.com/markheydon/solo-dev-board/issues/292), [DEC-038](adr/adr-0038-custom-actions-template-sources.md)) — one GitHub YAML source via catalogue selector or manual field, merged into Actions Templates; last-used localStorage only
- Triage action-surface usability ([#492](https://github.com/markheydon/solo-dev-board/issues/492)) — Save and next / duplicate / skip layout only

**Out of milestone:** Window-focus refetch ([#450](https://github.com/markheydon/solo-dev-board/issues/450), ice-box); hosted private Projects v2 ([#293](https://github.com/markheydon/solo-dev-board/issues/293)); Aspire PM settings store ([#391](https://github.com/markheydon/solo-dev-board/issues/391)); newer ice-box catalogue and hygiene work ([#381](https://github.com/markheydon/solo-dev-board/issues/381), [#397](https://github.com/markheydon/solo-dev-board/issues/397), [#411](https://github.com/markheydon/solo-dev-board/issues/411) mobile layout under [#527](https://github.com/markheydon/solo-dev-board/issues/527), [#435](https://github.com/markheydon/solo-dev-board/issues/435)–[#439](https://github.com/markheydon/solo-dev-board/issues/439), [#470](https://github.com/markheydon/solo-dev-board/issues/470)).

**Status:** Complete (2026-09-07). Tagged [`v1.2.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.2.0). Production CD and GitHub Pages publish from `v*` tags.

**Target:** Not set — dates follow actual start per Roadmap date guidelines.

---

### v1.3 - Label remap onto the recommended taxonomy

**Goal:** Let existing repository catalogues adopt the recommended label taxonomy without wiping issue and pull request history.

**Scope:**
- Label Manager preview-first remap of extras onto taxonomy (or other existing) names ([#491](https://github.com/markheydon/solo-dev-board/issues/491), [#519](https://github.com/markheydon/solo-dev-board/issues/519)–[#521](https://github.com/markheydon/solo-dev-board/issues/521))
- Remap apply and delete safety fixes ([#548](https://github.com/markheydon/solo-dev-board/issues/548), [#550](https://github.com/markheydon/solo-dev-board/issues/550), [#556](https://github.com/markheydon/solo-dev-board/issues/556))
- Production deploy on persistent tier Key Vaults ([ADR-0045](adr/adr-0045-persistent-key-vault-dual-path-local-secrets.md), [#583](https://github.com/markheydon/solo-dev-board/pull/583)) shipped with this tag

**Out of milestone:** Further solo-workflow ideas from dogfood were not added to this increment.

**Status:** Complete (2026-11-15). Tagged [`v1.3.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.3.0). Milestone #9 closed.

**Documentation gap:** The tag went out without a full User Guide pass or refreshed screenshots for label remap. Cleared on **`v1.4.0`** (full `website/static/images/` recapture). See [Release runbook](#release-runbook).

**Target:** Not set — dates follow actual start per Roadmap date guidelines.

---

### v1.4 - Product chrome, branding, and narrow viewports

**Goal:** Shared MudBlazor page chrome, a narrow-viewport pass, and product identity (display name, logo or mark, favicon set, and token cleanup) so daily use feels like one product.

**Scope:**
- Epic [#526](https://github.com/markheydon/solo-dev-board/issues/526)
- Visual language: feature [#527](https://github.com/markheydon/solo-dev-board/issues/527), story [#528](https://github.com/markheydon/solo-dev-board/issues/528), test [#530](https://github.com/markheydon/solo-dev-board/issues/530) (closed on the milestone)
- Narrow viewports: story [#411](https://github.com/markheydon/solo-dev-board/issues/411) (closed on the milestone)
- Branding, pulled forward on 2026-11-15 (no separate `v1.5` milestone): feature [#397](https://github.com/markheydon/solo-dev-board/issues/397), enabler [#529](https://github.com/markheydon/solo-dev-board/issues/529), test [#531](https://github.com/markheydon/solo-dev-board/issues/531)

**Delivery mode:** Pre–Spec Kit legacy path (existing issues only; no new child-issue prompts). See [`SPEC_KIT_MIGRATION.md`](SPEC_KIT_MIGRATION.md).

**Out of milestone:** Ice-box catalogue features; platform-blocked chores ([#293](https://github.com/markheydon/solo-dev-board/issues/293), [#391](https://github.com/markheydon/solo-dev-board/issues/391), [#498](https://github.com/markheydon/solo-dev-board/issues/498)).

**Status:** Complete (2026-11-15). Tagged [`v1.4.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.4.0). Milestone [#10](https://github.com/markheydon/solo-dev-board/milestone/10) closed. User Guide and full screenshot pass on `main` before tag.

**After `v1.4.0`:** Spec Kit is the default for new feature work; `plan/` delivery bundles for shipped increments live under [`archive/`](archive/README.md) (see [`SPEC_KIT_MIGRATION.md`](SPEC_KIT_MIGRATION.md)).

**Planning bundle (archived):** [`archive/product-chrome-ui-quality/README.md`](archive/product-chrome-ui-quality/README.md).

**Target:** Shipped 2026-11-15.

---

### v1.5

Not declared. Product branding was folded into **`v1.4`** on 2026-11-15. Do not open a `v1.5 - Product branding` milestone.

---

## Release runbook

This is the release checklist. Do not tag until every item below is done, or the release notes name an explicit waiver. `v1.3.0` skipped the User Guide and screenshot pass; that miss is why this list is mandatory.

```
Feature branch → Pull Request → CI passes → Code review → Merge to main → CD deploys to staging → User Guide and screenshots on main → Tag release → CD deploys to production + Pages publish
```

GitHub Pages publishes the public site only from the `v*` tag. A guide update that lands after the tag does not appear on solodevboard.com until the next tag.

### Before the tag

1. **Milestone scope:** Every issue on the milestone is closed, or explicitly moved off with a note in this file.
2. **Merge to `main`:** Release work is merged via PRs with CI passing. CD deploys to the **staging** Azure Container Apps environment.
3. **Staging smoke test:** Open **More options → About**. Confirm a `staging` pre-release suffix, a **Built** timestamp for this deploy, and a **Build** SHA that matches `main`.
4. **User Guide (required):** On `main`, before the tag:
   - Update every page under `website/content/docs/` whose shipped behaviour changed, plus `website/content/_index.md` and `website/content/docs/_index.md` when the feature list or guide index changes.
   - When shared chrome, navigation, theme, or the product display name changed, review **every** published guide page and landing tile, not only the feature that shipped.
   - Catch up any guide or screenshot debt from the previous tag. For the next tag after `v1.3.0`, that includes label remap and any chrome already on `main`.
   - Recapture screenshots for every guide image whose screen changed. Follow [Screenshot convention](DOCS_STRATEGY.md#screenshot-convention): light theme, 1400×900, drawer collapsed, docs capture mode, public catalogues only, loaded states with `markheydon/solo-dev-board`. Prefer `DOCS_CAPTURE_ENABLED=1 dotnet test tests/SoloDevBoard.E2E.Tests --filter "Category=DocsCapture"` against a local app with a real PAT and `DocsCapture:Enabled=true`. If capture cannot run, say so in the release notes and do not pretend the images are current.
   - When chrome, navigation, or theme changed across the app, recapture the whole of `website/static/images/`, because landing tiles and guide heroes share those shots.
   - Update [`tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md`](../tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md) when routes, locking-test pointers, or capture files change.
   - Run `./scripts/invoke-hugo-site.sh build` (or rely on `hugo-validate` on the docs PR). Pages still does not publish until the tag.
5. **Changelog and this file:** Move `[Unreleased]` notes in [`CHANGELOG.md`](../CHANGELOG.md) into a dated `## [X.Y.Z]` section. Set this release's **Status** to the tag and date.
6. **Operator docs:** If configuration or deploy steps changed, update `docs/` in the same change. See [`DOCS_STRATEGY.md`](DOCS_STRATEGY.md).

### Tag and publish

7. **Tag the release** (example):
   ```bash
   git tag -a v1.4.0 -m "Release v1.4.0 - Product chrome, branding, and narrow viewports"
   git push origin v1.4.0
   ```
8. **Create a GitHub Release** from that tag. Title format: `v1.4.0 - Product chrome, branding, and narrow viewports`. Notes cover what is new, what is fixed, known issues, and any waived docs item. Pushing the `v*` tag triggers production CD and GitHub Pages publish.
9. **Close the milestone** after the tag exists and the milestone issues are closed.

### Hotfix Process

For urgent bug fixes between releases:
1. Branch from `main`: `git checkout -b hotfix/describe-the-fix`.
2. Fix and test.
3. PR to `main` with label `type/bug` and `priority/critical`.
4. After merging, tag a patch release: `v1.4.1`.
5. If the patch changes visible UI or documented behaviour, repeat the User Guide and screenshot items before the patch tag. A code-only patch with no visible or documented change can skip screenshot recapture; say that in the release notes.

---

## Creating a GitHub Release

GitHub Releases are created from the GitHub web interface or using the GitHub CLI:

```bash
# Using the GitHub CLI
gh release create v0.2.0 \
  --title "v0.2.0 - Core Features" \
  --notes "## What's New

### Label Manager
- ...

### Audit Dashboard
- ...

## Bug Fixes
- ...

## Known Issues
- ..."
```

---

## AI Collaborator Instructions

- Follow the [Release runbook](#release-runbook) before tagging. Do not tag while the User Guide or screenshots are still outstanding.
- When a phase is complete, update the **Target** section of the relevant release entry to reflect the actual completion date.
- When a new GitHub Release is created, update the milestone status in `plan/PROJECT_MANAGEMENT.md`.
- When drafting release notes, pull completed items from closed GitHub Issues on the relevant milestone.
