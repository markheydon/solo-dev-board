<!--
Sync Impact Report
- Version change: (placeholder scaffold) → 1.0.0
- Modified principles: N/A (initial ratification from brownfield analysis)
- Added sections: Core Principles (5); Platform and Security Constraints; Delivery Workflow and Quality Gates; Governance
- Removed sections: Template placeholder tokens
- Deferred items: resolve-template.sh failed (Python stdlib `json` unavailable); scaffold taken from `.specify/templates/constitution-template.md`
- Archive note: `plan/archive/` informed wording only; no mandatory legacy PM workflows
-->

# SoloDevBoard Constitution

## Core Principles

### I. Layered architecture and dependency discipline

SoloDevBoard MUST maintain clean layered boundaries: **Domain** (no outward dependencies), **Application** (Domain only), **Infrastructure** (implements Application contracts), **Composition** (sole DI root via `AddSoloDevBoard`), and **Web** (Application and Composition only; MUST NOT reference Infrastructure in project references or `using` directives).

Business logic MUST live in Application services, not in Razor components. Web components MUST call Application interfaces only. Constructor injection MUST be used; service-locator patterns are forbidden. Public members MUST have XML documentation (`///`). Guard clauses MUST use `ArgumentNullException.ThrowIfNull()`.

**Rationale:** The brownfield codebase (376 production C# files across seven `src/` projects, 46 ADRs) encodes this as the primary defence against UI–domain coupling and untestable GitHub API calls. It matches ADR-0004 and enables NSubstitute-based unit tests without HTTP.

### II. Boundary data shapes and proportional design

Repository interfaces MUST return domain entity records. Public Application service interfaces (`I*Service`, `I*Manager`) consumed by **Web** MUST accept and return **Application DTO records** only (`<Entity>Dto`); domain types MUST NOT appear on those public surfaces. Mapping belongs in Application services, not Razor or AutoMapper.

New abstractions (projects, interfaces, frameworks) MUST pass a proportionality check: the smallest correct home wins; speculative layers and copy-paste vertical slices are rejected. Features that materially alter user-facing pages SHOULD align with wireframes under `plan/wireframes/` when present.

**Rationale:** ADR-0011 closes the gap left by layering rules alone. AI-assisted development makes implicit boundaries drift; explicit DTO boundaries keep components stable when domain models change.

### III. Formal testing standard and docs alignment

Automated tests MUST use **xUnit v3** (Microsoft.Testing.Platform), **NSubstitute** for doubles, and **built-in `Assert` only**. **bUnit** is limited to `SoloDevBoard.Web.Tests`; **Playwright** E2E lives in `SoloDevBoard.E2E.Tests`. Moq, FluentAssertions, NUnit, MSTest, and AwesomeAssertions are banned.

Tests MUST follow `MethodUnderTest_Scenario_ExpectedOutcome` naming with Arrange / Act / Assert sections. .NET Aspire AppHost modelling MUST NOT be unit-tested. Fast CI and local paths MUST use `SoloDevBoard.UnitTests.slnf` (ADR-0041); E2E is owned by `playwright.yml` with category filters, not as the default `dotnet test` on the full solution.

Published end-user guides under `website/content/docs/` MUST stay aligned with Playwright coverage per `tests/SoloDevBoard.E2E.Tests/USER_DOCS_ALIGNMENT.md` and `CRITICAL_JOURNEYS.md`. New or changed user-facing behaviour MUST update the guide, journey inventory, and tests in the same change when the behaviour is user-visible.

**Rationale:** ADR-0042, ADR-0040, ADR-0041, and ADR-0010 define a tiered pyramid (232 test C# files today) that keeps CI under 15 minutes while protecting shell, feature chrome, and accessibility journeys.

### IV. Consistent product chrome and accessible UX

All operational pages MUST share one MudBlazor visual language (ADR-0039): page header with purpose line, toolbar for primary commands, `MudPaper` / `MudCard` at elevation 1, `MudSkeleton` for content-shaped loading, `MudAlert` for persistent empty and error state, and `ISnackbar` for transient outcomes only (ADR-0035; no duplicate snackbar plus inline alert for the same outcome).

Agents and contributors MUST consult the `mudblazor` skill before new UI. Prefer MudBlazor components and utilities before `.razor.css`. Custom CSS is exceptional. `MudChat`, FABs, carousels, and charts MUST NOT appear on operational pages. Icon-only controls MUST have accessible names. Primary journeys MUST remain usable on narrow viewports (responsive web, not native apps).

WCAG 2.1 Level A and AA violations classified **critical** or **serious** by axe in CI MUST be fixed before merge. Stable `data-testid` hooks MUST be preserved or wrapped when restructuring chrome to avoid unnecessary E2E churn.

**Rationale:** Archive QA bundles (e.g. product chrome UI quality) and live E2E specs encode chrome as a cross-feature contract, not per-page taste.

### V. Performance-conscious I/O and observable operations

All I/O-bound Application and Infrastructure APIs MUST be async and MUST accept `CancellationToken` on public async surfaces. Sync-over-async (`.Result`, `.Wait()`) is forbidden.

Read-heavy GitHub catalogue data (repositories, labels, milestones) MUST be cached in Infrastructure with scoped `IMemoryCache` keys tied to the current user's owner login; invalidate on matching mutations (ADR-0044). Application-layer DTO caching, distributed cache for this tranche, and caching of issues, pull requests, workflow runs, or GraphQL project queries are out of scope until a follow-up ADR.

Service defaults MUST keep health checks, structured logging, and OpenTelemetry export via `SoloDevBoard.ServiceDefaults` for hosted and Aspire paths. CI MUST stay split across focused workflows (ADR-0030) rather than one monolithic pipeline.

**Rationale:** Blazor Server multi-repo workloads amplify GitHub latency; caching and async discipline are product requirements, not optimisations of last resort.

## Platform and Security Constraints

- **Stack (fixed for v1.x):** .NET 10 LTS (`net10.0`), C# 14, Blazor Server, MudBlazor, Aspire AppHost for local orchestration and Azure Container Apps deployment (ADR-0001, ADR-0012, ADR-0018). Replacing Blazor Server with a SPA stack requires a major ADR and scope revision (GOALS kill criteria).
- **Quality bar on build:** Nullable reference types and implicit usings enabled; `dotnet format SoloDevBoard.slnx --verify-no-changes` and `dotnet build SoloDevBoard.slnx -c Release -warnaserror` MUST pass before merge-quality work ships.
- **Secrets:** Credentials MUST NOT be committed. Runtime secrets use Aspire user secrets, Key Vault at deploy, or documented operator paths (ADR-0043, ADR-0045). GitHub Actions CD MUST use OIDC for Azure, not long-lived cloud keys in source control.
- **Hosted access:** Public deployments MUST enforce operator-managed admission control (deny-by-default) before repository data loads (ADR-0014).
- **Documentation language:** UK English in user-facing strings, comments, and docs; the Unicode em dash (U+2014) MUST NOT appear in repository prose.
- **Decisions:** Significant architecture or technology choices MUST be recorded under `adr/`; rejected options in archive or legacy logs do not override active ADRs.
- **Out of product scope unless replanned:** Non-GitHub providers, team collaboration features, native mobile clients, marketplace monetisation in v1.0 (see `SCOPE.md` / `plan/SCOPE.md`).

## Delivery Workflow and Quality Gates

- **Spec Kit ownership:** Feature intent for new work flows through Spec Kit artefacts (`spec.md`, `plan.md`, `tasks.md` under `.specify/`). Those artefacts MUST NOT contradict this constitution. Detailed day-to-day coding rules remain in `AGENTS.md` and `CONVENTIONS.md` and MUST be kept consistent with amended principles.
- **Pull requests:** MUST follow `plan/PULL_REQUEST_POLICY.md` (title shape, template body, labels). Agent-authored PRs MUST receive human review before merge.
- **Pre-merge verification (minimum):** `dotnet format` verify, Release build with warnings as errors, `dotnet test SoloDevBoard.UnitTests.slnf -c Release --no-build` when code or tests change; Playwright-impacting UI changes MUST consider `playwright.yml` tiers and journey docs.
- **Historical artefacts:** Material under `plan/archive/` (Copilot-era bundles, weekly PM notes, retired implementation plans) MAY inform context but MUST NOT be mandated as ongoing process. Open work lives in GitHub Issues and Project #8, not new archive bundles.
- **Agent skills:** `.agents/skills/` (e.g. `implementation-guardrails`, `mudblazor`, `code-review`) supplement the constitution during implementation; they do not replace ADRs or quality gates.

## Governance

This constitution is the **non-negotiable governance layer** for Spec Kit-driven feature work on SoloDevBoard. It codifies brownfield practice distilled from `GOALS.md`, `CONVENTIONS.md`, `adr/`, CI workflows, E2E journey definitions, and archived planning history (without requiring that history's workflows going forward).

**Hierarchy when guidance conflicts:**

1. This constitution (principles and MUST rules).
2. Active ADRs in `adr/` for the specific decision domain.
3. `AGENTS.md`, `CONVENTIONS.md`, and `plan/PULL_REQUEST_POLICY.md` for operational detail.
4. Per-feature Spec Kit specs and plans (MUST comply with 1–3).

**Amendments:** Changes MUST go through `/speckit-constitution`, bump `CONSTITUTION_VERSION` using semantic versioning (MAJOR for removed or redefined principles; MINOR for new principles or material expansions; PATCH for clarifications only), set `LAST_AMENDED_DATE` to the amendment date, and include a Sync Impact Report comment until the amended file is committed.

**Compliance review:** Authors and reviewers MUST verify feature specs, tasks, and PRs against relevant principles. Complexity beyond existing patterns MUST be justified in the spec or an ADR. Constitution violations MUST be fixed or explicitly escalated for amendment, not silently ignored.

**Runtime guidance:** Use `AGENTS.md` and `CONVENTIONS.md` for command sequences, file placement, and documentation sync tables; use `adr/` for decision history; use `.agents/skills/` during implementation.

**Version**: 1.0.0 | **Ratified**: 2026-11-15 | **Last Amended**: 2026-11-15