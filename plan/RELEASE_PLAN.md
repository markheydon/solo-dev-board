# SoloDevBoard — Release Plan

## Versioning Strategy

SoloDevBoard follows [Semantic Versioning](https://semver.org/) (`MAJOR.MINOR.PATCH`):

- **MAJOR** — Breaking changes to public APIs, configuration schemas, or deployment requirements.
- **MINOR** — New features added in a backwards-compatible manner.
- **PATCH** — Backwards-compatible bug fixes and minor improvements.

During the pre-1.0 development phase (`0.x.y`), minor version bumps may include breaking changes as the application stabilises.

### Build-time versioning (MinVer)

Application version numbers are calculated automatically at build time by [MinVer](https://github.com/adamralph/minver) from git tags on `SoloDevBoard.App`. The About page and GitHub API user-agent both read this stamped assembly metadata.

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

**Target:** End of Phase 1 (see [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md))

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

**Sequencing note:** Selected hosted-authentication and Azure-delivery items were pulled forward to support safe hosted validation. The six core tools shipped in v1.0.0. Post-1.0 delivery uses a single active milestone model ([DEC-027](DECISIONS.md#dec-027-post-10-milestone-and-work-item-hierarchy)).

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

**Out of milestone (backlog):** Private user-owned Projects v2 via hosted sign-in ([#293](https://github.com/markheydon/solo-dev-board/issues/293)) — platform-blocked; see [`plan/GITHUB_PROJECTS_V2_ACCESS.md`](GITHUB_PROJECTS_V2_ACCESS.md). Product operating system overlay ([#475](https://github.com/markheydon/solo-dev-board/issues/475)) — ice-boxed direction; see [`plan/PRODUCT_OPERATING_SYSTEM.md`](PRODUCT_OPERATING_SYSTEM.md). Window-focus refetch ([#450](https://github.com/markheydon/solo-dev-board/issues/450)) stays off `v1.2`.

**Status:** Complete (2026-08-31). Tagged [`v1.1.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.1.0). Production CD and GitHub Pages publish from `v*` tags.

**Target:** Early September 2026 (met)

---

### v1.2 - Planning polish, Reload & Templates

**Goal:** Finish older unfinished slices after `v1.1.0`: Planning follow-through, Reload from GitHub, hosted-auth HttpClient hygiene, custom workflow template repositories, and Triage action-surface usability.

**Scope:**
- Optional board-scoped Daily Focus recommendations ([#403](https://github.com/markheydon/solo-dev-board/issues/403))
- Reload from GitHub, keep selection ([#447](https://github.com/markheydon/solo-dev-board/issues/447), [#449](https://github.com/markheydon/solo-dev-board/issues/449), [#451](https://github.com/markheydon/solo-dev-board/issues/451))
- Split hosted GitHub OAuth and API HttpClients ([#453](https://github.com/markheydon/solo-dev-board/issues/453))
- Custom workflow template repositories ([#292](https://github.com/markheydon/solo-dev-board/issues/292)) — stay on this milestone to force a decision (implement, reshape, or explicitly drop), not another ice-box kick
- Triage action-surface usability ([#492](https://github.com/markheydon/solo-dev-board/issues/492)) — Save and next / duplicate / skip layout only

**Out of milestone:** Window-focus refetch ([#450](https://github.com/markheydon/solo-dev-board/issues/450), ice-box); hosted private Projects v2 ([#293](https://github.com/markheydon/solo-dev-board/issues/293)); Aspire PM settings store ([#391](https://github.com/markheydon/solo-dev-board/issues/391)); newer ice-box catalogue and hygiene work ([#381](https://github.com/markheydon/solo-dev-board/issues/381), [#397](https://github.com/markheydon/solo-dev-board/issues/397), [#411](https://github.com/markheydon/solo-dev-board/issues/411), [#435](https://github.com/markheydon/solo-dev-board/issues/435)–[#439](https://github.com/markheydon/solo-dev-board/issues/439), [#470](https://github.com/markheydon/solo-dev-board/issues/470)).

**Status:** Open (declared 2026-08-31). GitHub milestone [`v1.2 - Planning polish, Reload & Templates`](https://github.com/markheydon/solo-dev-board/milestone/8). Release tag `v1.2.0` when shipped.

**Target:** Not set — dates follow actual start per Roadmap date guidelines.

---

## Release Process

```
Feature branch → Pull Request → CI passes → Code review → Merge to main → CD deploys to staging → Tag release → CD deploys to production + Pages publish
```

### Step-by-Step

1. **Merge to `main`:** All features for the release are merged via PRs with the CI pipeline passing. CD deploys automatically to the **staging** Azure Container Apps environment.
2. **Final smoke test:** Verify the deployment to the staging environment is healthy. Open **More options → About** and confirm the version shows a `staging` pre-release suffix, the **Built** timestamp reflects the latest deploy, and the **Build** commit SHA matches the latest commit on `main` for that deploy.
3. **Update documentation:** Ensure all user-facing docs on `main` reflect the released features (validated by `hugo-validate` on PRs; not published until tagged).
4. **Tag the release:**
   ```bash
   git tag -a v0.2.0 -m "Release v0.2.0 - Core Features"
   git push origin v0.2.0
   ```
5. **Create a GitHub Release:**
   - Navigate to the repository on GitHub → Releases → Draft a new release.
   - Select the tag created above.
   - Use the release title format: `v0.2.0 - Core Features`.
   - Write release notes describing what's new, what's fixed, and any known issues.
   - Attach build artefacts if applicable.
   - Publish the release.
   - Pushing the `v*` tag triggers production CD and GitHub Pages publish automatically.
6. **Close the milestone:** Close the corresponding GitHub milestone (e.g. "Phase 2 — v0.2.0").

### Hotfix Process

For urgent bug fixes between releases:
1. Branch from `main`: `git checkout -b hotfix/describe-the-fix`.
2. Fix and test.
3. PR to `main` with label `type/bug` and `priority/critical`.
4. After merging, tag a patch release: `v0.2.1`.

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

- When a phase is complete, update the **Target** section of the relevant release entry to reflect the actual completion date.
- When a new GitHub Release is created, update the milestone status in `plan/PROJECT_MANAGEMENT.md`.
- When drafting release notes, pull completed items from closed GitHub Issues on the relevant milestone.
