# Archived planning artefacts

Material here is **historical or superseded**. Active planning uses [GitHub Issues](https://github.com/markheydon/solo-dev-board/issues), [Project #8](https://github.com/users/markheydon/projects/8), and the lean docs in the parent [`plan/`](../README.md) folder. New feature intent will move to GitHub Spec Kit when adopted ([`SPEC_KIT_MIGRATION.md`](../SPEC_KIT_MIGRATION.md)).

Paths in archived files may use pre-2026-11 layouts (`src/App/SoloDevBoard.App`, nested `src/Application/`, or `tests/App.Tests/`). Current equivalents are `src/SoloDevBoard.Web/`, `src/SoloDevBoard.Application/`, and `tests/SoloDevBoard.Web.Tests/` (see [ADR-0004](../../adr/adr-0004-layered-architecture.md)).

## What lives here

| Location | Contents |
|----------|----------|
| [`copilot-era/`](copilot-era/) | Retired multi-agent PM workflow: runbook, weekly progress reviews, and per-feature delivery bundles (project plans, issue checklists, QA and test plans). |
| [`copilot-era/feature-bundles/`](copilot-era/feature-bundles/) | Completed-feature planning packs from the Copilot era. GitHub Issues remain the source of truth. |
| [`copilot-era/weekly-updates/`](copilot-era/weekly-updates/) | Dated PM progress review notes. |
| [`BACKLOG-2026-07-18.md`](BACKLOG-2026-07-18.md) | Full backlog snapshot before Issues migration. |
| [`BACKLOG_TO_ISSUES_MIGRATION.md`](BACKLOG_TO_ISSUES_MIGRATION.md) | Migration record (complete 2026-07-18). |
| [`IMPLEMENTATION_PLAN.md`](IMPLEMENTATION_PLAN.md) | v0.x–v1.2 phased implementation history (all tasks complete). |
| [`HOSTED_AUTH_MIGRATION_STRATEGY.md`](HOSTED_AUTH_MIGRATION_STRATEGY.md) | Hybrid-to-GitHub-App-first hosted auth migration (executed). |
| [`HARDCODING_AUDIT_v1.1.md`](HARDCODING_AUDIT_v1.1.md) | v1.1 maintainer-identity audit ([#423](https://github.com/markheydon/solo-dev-board/issues/423)). |
| [`ASPIRE_MULTI_PROCESS_FINDINGS.md`](ASPIRE_MULTI_PROCESS_FINDINGS.md) | Optional future Aspire multi-process notes (not a decision). |
| [`POST_KEY_VAULT_MIGRATION_CLEANUP.md`](POST_KEY_VAULT_MIGRATION_CLEANUP.md) | ADR-0045 legacy Key Vault and empty Aspire resource group cleanup (complete 2026-11-15). |
| [`label-remap-taxonomy-project-plan.md`](label-remap-taxonomy-project-plan.md) | Label remap delivery bundle (shipped `v1.3.0`). |
| [`product-chrome-ui-quality/`](product-chrome-ui-quality/README.md) | Product chrome, branding, and narrow viewports (shipped `v1.4.0`). |

Do not add new delivery bundles here; use Spec Kit specs (`specs/<feature>/`) instead.

**Re-archive review (2026-11-15):** Shipped pre–Spec Kit bundles moved from active `plan/` per [`SPEC_KIT_MIGRATION.md`](../SPEC_KIT_MIGRATION.md). Post-ship archival specs under `specs/` remain optional.