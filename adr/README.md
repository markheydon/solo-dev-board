# Architectural Decision Records

ADRs capture significant design decisions for Solo Dev Board.

**Filename:** `adr-NNNN-[title-slug].md` (four-digit sequence).

**Authoring:** Use the `create-architectural-decision-record` skill in [`.agents/skills/`](../.agents/skills/).

**Retired decision log:** [`plan/DECISIONS.md`](../plan/DECISIONS.md) (pointer only).

## How agents should use this folder

1. Read this page **first** (status legend and index below).
2. Open a full ADR file only when the index row is **Binding** and the topic matches your task.
3. Do **not** implement guidance from **Historical only** or **Superseded** rows unless the task is explicitly archival or migration.

When you add an ADR or change its status, **update the index table on this page** in the same change.

## Status field

Every ADR must include a line immediately after the title block: `**Status:** <value>`.

Use **only** these values (amendments stay inline, for example `Accepted (amended YYYY-MM-DD: …)`):

| Status | Meaning | Agent note |
| --- | --- | --- |
| **Proposed** | Under review; not binding | Do not implement |
| **Accepted** | Binding architecture or platform decision | Binding |
| **Active** | Binding product, process, or feature rule; amend via addendum | Binding |
| **Superseded** | Replaced by a later ADR | See successor ADR only |
| **Withdrawn** | Abandoned before or without full implementation; no successor yet | Historical only — do not implement |
| **Rejected** | Formally considered and not chosen | Historical only — do not implement |
| **Deprecated** | Still visible in old code; must not extend | Do not extend — migrate off |

### Accepted vs Active

Both are **binding** for implementation.

- **Accepted:** stack, deployment, security boundaries, cross-cutting architecture.
- **Active:** feature behaviour, UI conventions, CI/process, rules that expect frequent addenda.

A future hygiene pass may converge labels; do not relabel existing ADRs without an explicit task.

## Index

| ADR | Title | Status | Agent note |
| --- | --- | --- | --- |
| [0001](adr-0001-blazor-server.md) | Use Blazor Server for the Front-End | Accepted | Binding |
| [0002](adr-0002-testing-framework.md) | Use xUnit and NSubstitute for Testing | Superseded | See [0006](adr-0006-moq-mocking-library.md), [0008](adr-0008-remove-fluentassertions.md) |
| [0003](adr-0003-bicep-infrastructure.md) | Use Bicep for Azure Infrastructure as Code | Superseded | See [0018](adr-0018-aspire-azure-container-apps-deployment.md) |
| [0004](adr-0004-layered-architecture.md) | Use Layered / Clean Architecture | Accepted | Binding |
| [0005](adr-0005-github-api-strategy.md) | GitHub API Strategy | Accepted | Binding |
| [0006](adr-0006-moq-mocking-library.md) | Switch Mocking Library to Moq | Accepted | Binding |
| [0007](adr-0007-multi-tenancy-authentication-phased-approach.md) | Multi-Tenancy Authentication — Phased Approach | Accepted | Binding |
| [0008](adr-0008-remove-fluentassertions.md) | Remove FluentAssertions | Accepted | Binding |
| [0009](adr-0009-fluent-ui-blazor-component-library.md) | Use Fluent UI Blazor | Superseded | See [0012](adr-0012-switch-to-mudblazor-component-library.md) |
| [0010](adr-0010-bunit-component-testing.md) | Use bUnit for Blazor Component Testing | Accepted | Binding |
| [0011](adr-0011-boundary-data-shapes.md) | Boundary Data Shapes (DTOs) | Accepted | Binding |
| [0012](adr-0012-switch-to-mudblazor-component-library.md) | Switch UI to MudBlazor | Accepted | Binding |
| [0013](adr-0013-one-click-migration-scope-and-preview-strategy.md) | One-Click Migration scope and preview | Accepted | Binding |
| [0014](adr-0014-hosted-access-control-for-public-deployments.md) | Hosted access control | Accepted | Binding |
| [0015](adr-0015-github-app-first-hosted-authentication.md) | GitHub App-first hosted authentication | Accepted | Binding |
| [0016](adr-0016-consider-aspire-for-local-orchestration-and-future-hosting.md) | Consider Aspire for orchestration | Accepted | Binding |
| [0017](adr-0017-github-actions-bridge-for-roadmap-project-sync.md) | GitHub Actions bridge for roadmap sync | Accepted | Binding |
| [0018](adr-0018-aspire-azure-container-apps-deployment.md) | Aspire Azure Container Apps deployment | Accepted | Binding |
| [0019](adr-0019-hugo-hextra-end-user-docs.md) | Hugo (Hextra) end-user docs | Active | Binding |
| [0020](adr-0020-public-only-docs-capture-mode.md) | Public-only docs capture mode | Active | Binding |
| [0021](adr-0021-two-tier-cd-pipeline.md) | Two-tier CD pipeline | Active | Binding |
| [0022](adr-0022-canonical-pull-request-policy.md) | Canonical pull request policy | Active | Binding |
| [0023](adr-0023-public-product-site-ia-and-domain.md) | Public product site IA and domain | Active | Binding |
| [0024](adr-0024-post-1-0-milestone-numbering.md) | Post-1.0 milestone numbering | Superseded | Historical only |
| [0025](adr-0025-optional-shared-azure-container-registry.md) | Optional shared Azure Container Registry | Active | Binding |
| [0026](adr-0026-project-8-archive-rule-via-roadmap-sync.md) | Project #8 archive rule | Active | Binding |
| [0027](adr-0027-post-1-0-milestone-and-work-item-hierarchy.md) | Post-1.0 milestone and work-item hierarchy | Active | Binding |
| [0028](adr-0028-blocked-and-ice-box-project-status-options.md) | Blocked and Ice Box status options | Active | Binding |
| [0029](adr-0029-cross-repo-planning-board-selection-and-local-settings.md) | Cross-Repo Planning board and local settings | Active | Binding |
| [0030](adr-0030-self-documenting-ci-workflow-split.md) | Self-documenting CI workflow split | Active | Binding |
| [0031](adr-0031-github-comment-workflow-invocation.md) | GitHub comment workflow invocation | Active | Binding |
| [0032](adr-0032-oss-catalogue-identification-from-open-source-topic.md) | OSS catalogue from `open-source` topic | Active | Binding |
| [0033](adr-0033-actions-templates-and-planning-product-names.md) | Actions Templates and Planning names | Active | Binding |
| [0034](adr-0034-label-manager-recommended-catalogue-omits-area-labels.md) | Label Manager catalogue omits `area/*` | Active | Binding |
| [0035](adr-0035-transient-feedback-via-snackbar.md) | Transient feedback via snackbar | Active | Binding |
| [0036](adr-0036-one-click-migration-label-overwrite-keeps-area.md) | Migration label overwrite keeps `area/*` | Active | Binding |
| [0037](adr-0037-one-click-migration-ignores-source-area-labels.md) | Migration ignores source `area/*` | Active | Binding |
| [0038](adr-0038-custom-actions-template-sources.md) | Custom Actions template sources | Active | Binding |
| [0039](adr-0039-mudblazor-visual-language-for-page-chrome.md) | MudBlazor page chrome | Active | Binding |
| [0040](adr-0040-c-playwright-e2e-suite.md) | C# Playwright E2E suite | Active | Binding |
| [0041](adr-0041-unit-e2e-test-tiering.md) | Unit/E2E test tiering | Active | Binding |
| [0042](adr-0042-formalised-testing-standard-xunit-v3-nsubstitute-playwright.md) | Formalised testing standard | Active | Binding |
| [0043](adr-0043-key-vault-backed-hosted-auth-secrets.md) | Key Vault-backed hosted auth secrets | Active | Binding |
| [0044](adr-0044-github-api-response-caching-in-infrastructure.md) | GitHub API response caching | Active | Binding |
| [0045](adr-0045-persistent-key-vault-dual-path-local-secrets.md) | Persistent Key Vault dual-path local secrets | Accepted | Binding |
| [0046](adr-0046-overnight-inventory-functions-and-sql.md) | Overnight inventory (Functions and Azure SQL) | Withdrawn | Historical only — do not implement |
| [0047](adr-0047-apphost-blazor-resource-name-web.md) | AppHost Blazor resource name `web` | Accepted | Binding |
| [0048](adr-0048-hosted-operator-snapshot-blob-storage.md) | Hosted operator snapshot blob storage | Accepted | Binding |
| [0049](adr-0049-scheduled-collection-functions.md) | Scheduled snapshot collection Functions worker | Accepted | Binding |