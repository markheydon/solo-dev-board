# SoloDevBoard — Release Plan

## Versioning Strategy

SoloDevBoard follows [Semantic Versioning](https://semver.org/) (`MAJOR.MINOR.PATCH`):

- **MAJOR** — Breaking changes to public APIs, configuration schemas, or deployment requirements.
- **MINOR** — New features added in a backwards-compatible manner.
- **PATCH** — Backwards-compatible bug fixes and minor improvements.

During the pre-1.0 development phase (`0.x.y`), minor version bumps may include breaking changes as the application stabilises.

---

## Release Roadmap

### v0.1.0 — Foundation / MVP

**Goal:** A working Blazor Server application deployed to Azure that can authenticate with GitHub and display a list of repositories.

**Scope:**
- GitHub PAT authentication
- Repository listing
- Empty dashboard shell (placeholder panels for all 6 features)
- CI pipeline
- Azure App Service deployment via Bicep

**Target:** End of Phase 1 (see [IMPLEMENTATION_PLAN.md](IMPLEMENTATION_PLAN.md))

---

### v0.2.0 — Core Features

**Goal:** Deliver the Label Manager and Audit Dashboard features.

**Scope:**
- Label Manager: view, create, edit, delete, and synchronise labels across repositories
- Audit Dashboard: open issues, stale PRs, workflow statuses, label inconsistencies

**Target:** End of Phase 2

---

### v0.3.0 — Migration and Triage

**Goal:** Deliver the One-Click Migration and Triage UI features.

**Scope:**
- One-Click Migration: labels and milestones
- Triage UI: keyboard-friendly issue triage with quick actions

**Target:** End of Phase 3

---

### v0.4.0 — Visualisation and Templates

**Goal:** Deliver the Board Rules Visualiser and Workflow Templates features.

**Scope:**
- Board Rules Visualiser: interactive diagram of project board automation rules
- Workflow Templates: built-in template library, apply to repositories

**Target:** End of Phase 4

---

### v1.0.0 — Production Ready

**Goal:** A stable, well-tested, and fully documented release suitable for regular use.

**Scope:**
- GitHub App authentication (replacing PAT for production)
- ≥80% unit test coverage
- Accessibility audit (WCAG 2.1 AA)
- Performance review and optimisation
- Complete user-facing documentation
- Full Azure deployment pipeline with environment gates

**Sequencing note:** Selected hosted-authentication and Azure-delivery items were pulled forward to support safe hosted validation. This did not change the main roadmap order, which still returns to the unfinished Phase 3 work first, then Phase 4, then Phase 5, before the remaining v1.0.0 work is completed.

**Target:** End of Phase 6

---

## Release Process

```
Feature branch → Pull Request → CI passes → Code review → Merge to main → CD deploys to production → Tag release
```

### Step-by-Step

1. **Merge to `main`:** All features for the release are merged via PRs with the CI pipeline passing.
2. **Final smoke test:** Verify the deployment to the production Azure App Service is healthy.
3. **Update documentation:** Ensure all user-facing docs reflect the released features.
4. **Tag the release:**
   ```bash
   git tag -a v0.2.0 -m "Release v0.2.0 — Core Features"
   git push origin v0.2.0
   ```
5. **Create a GitHub Release:**
   - Navigate to the repository on GitHub → Releases → Draft a new release.
   - Select the tag created above.
   - Use the release title format: `v0.2.0 — Core Features`.
   - Write release notes describing what's new, what's fixed, and any known issues.
   - Attach build artefacts if applicable.
   - Publish the release.
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
  --title "v0.2.0 — Core Features" \
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
- When drafting release notes, pull completed items from `plan/BACKLOG.md` for the relevant phase.
