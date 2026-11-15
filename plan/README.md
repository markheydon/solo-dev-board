# Maintainer and PM documentation (`plan/`)

Lean governance, operator notes, and UI wireframes for SoloDevBoard. **Open work lives in [GitHub Issues](https://github.com/markheydon/solo-dev-board/issues)** and [Project #8](https://github.com/users/markheydon/projects/8), not in markdown queues.

## Start here

| Doc | Purpose |
|-----|---------|
| [SCOPE.md](SCOPE.md) | In-scope and out-of-scope product boundaries |
| [BACKLOG.md](BACKLOG.md) | Pointer to GitHub Issues, Project #8, and milestones (not a work queue) |
| [RELEASE_PLAN.md](RELEASE_PLAN.md) | Versioning, [release runbook](RELEASE_PLAN.md#release-runbook), historical release notes |
| [SPEC_KIT_MIGRATION.md](SPEC_KIT_MIGRATION.md) | Spec Kit adoption — **default for new features** since `v1.4.0` |

## Governance and process

| Doc | Purpose |
|-----|---------|
| [LABEL_STRATEGY.md](LABEL_STRATEGY.md) | Issue and PR label taxonomy |
| [PULL_REQUEST_POLICY.md](PULL_REQUEST_POLICY.md) | PR titles, labels, and metadata |
| [PROJECT_MANAGEMENT.md](PROJECT_MANAGEMENT.md) | Issues, milestones, hierarchy ([DEC-027](adr/adr-0027-post-1-0-milestone-and-work-item-hierarchy.md)) |
| [PROJECT_BOARD_DESIGN.md](PROJECT_BOARD_DESIGN.md) | Project #8 columns, Status options, Roadmap Sync |
| [PROJECT_README.md](PROJECT_README.md) | Paste-ready text for the Project #8 info pane |
| [DOCS_STRATEGY.md](DOCS_STRATEGY.md) | How `docs/`, `website/`, and plan docs fit together |
| [DECISIONS.md](DECISIONS.md) | Retired pointer — use [`adr/`](../adr/) for decisions |
| [PRODUCT_OPERATING_SYSTEM.md](PRODUCT_OPERATING_SYSTEM.md) | Deferred product-catalogue direction ([#475](https://github.com/markheydon/solo-dev-board/issues/475)) |

## Operator and quality

| Doc | Purpose |
|-----|---------|
| [CURSOR_CLOUD.md](CURSOR_CLOUD.md) | Cursor Cloud VM and secrets mapping |
| [KEY_VAULT_SECRET_INVENTORY.md](KEY_VAULT_SECRET_INVENTORY.md) | Key Vault secret names |
| [HOSTED_AUTH_KEY_VAULT_PATTERN.md](HOSTED_AUTH_KEY_VAULT_PATTERN.md) | Hosted auth secrets pattern |
| [archive/POST_KEY_VAULT_MIGRATION_CLEANUP.md](archive/POST_KEY_VAULT_MIGRATION_CLEANUP.md) | Post-migration Azure cleanup (complete 2026-11-15) |
| [HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md](HOSTED_AUTH_SESSION_AND_TOKEN_FLOW.md) | Hosted session and token flow |
| [GITHUB_PROJECTS_V2_ACCESS.md](GITHUB_PROJECTS_V2_ACCESS.md) | Hosted private Projects v2 limitation |
| [ACCESSIBILITY_AUDIT.md](ACCESSIBILITY_AUDIT.md) | WCAG expectations for E2E |
| [OPERATIONAL_HARDENING_TEST_COVERAGE.md](OPERATIONAL_HARDENING_TEST_COVERAGE.md) | Operational E2E expectations |

## Feature references

| Doc | Purpose |
|-----|---------|
| [wireframes/](wireframes/) | Page layout references ([`CONVENTIONS.md`](../CONVENTIONS.md)) |

**New product intent:** GitHub Spec Kit (`specs/<feature>/`) — see [`SPEC_KIT_MIGRATION.md`](SPEC_KIT_MIGRATION.md). Shipped delivery bundles live under [`archive/`](archive/README.md) (for example [`archive/product-chrome-ui-quality/`](archive/product-chrome-ui-quality/README.md) for `v1.4`).

## Archive

Historical Copilot-era bundles, shipped milestone plans (`v1.3` label remap, `v1.4` chrome), weekly PM reviews, phased implementation plan, and one-off migration notes: [`archive/`](archive/README.md).