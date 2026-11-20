<!--
Sync Impact Report
- Version change: 1.1.1 → 1.1.2
- Modified principles: none
- Added principles: none
- Added sections: none
- Removed sections: none
- Deferred items: none
- Clarifications: Delivery Workflow — Aspire workflow skills via Cursor plugin (`.cursor/settings.json`), not vendored under `.agents/skills/`
-->

# SoloDevBoard Constitution

## Core Principles

### I. Layered architecture and dependency discipline

SoloDevBoard MUST maintain clean layered boundaries: **Domain** (no outward dependencies), **Application** (Domain only), **Infrastructure** (implements Application contracts), **Composition** (sole DI root via `AddSoloDevBoard`), and **Web** (Application and Composition only; MUST NOT reference Infrastructure in project references or `using` directives).

Business logic MUST live in Application services, not in Razor components. Web components MUST call Application interfaces only. Constructor injection MUST be used; service-locator patterns are forbidden. Public members MUST have XML documentation (`///`). Guard clauses MUST use `ArgumentNullException.ThrowIfNull()`.

**Rationale:** The brownfield codebase (376 production C# files across seven `src/` projects, 46 ADRs) encodes this as the primary defence against UI–domain coupling and untestable GitHub API calls. It matches ADR-0004 and enables NSubstitute-based unit tests without HTTP.

### II. Boundary data shapes and proportional design

Repository interfaces MUST return domain entity records. Public Application service interfaces (`I*Service`, `I*Manager`) consumed by **Web** MUST accept and return **Application DTO records** only (`<Entity>Dto`); domain types MUST NOT appear on those public surfaces. The internal GitHub gateway `IGitHubService` and repository interfaces (`I*Repository`) MAY remain domain-typed and MUST be consumed only from Application and Infrastructure; **Web** MUST NOT inject or reference `IGitHubService`. Mapping belongs in Application services, not Razor or AutoMapper.

New abstractions (projects, interfaces, frameworks) MUST pass a proportionality check: the smallest correct home wins; speculative layers and copy-paste vertical slices are rejected. Features that materially alter user-facing pages SHOULD align with wireframes under `plan/wireframes/` when present.

**Rationale:** ADR-0011 closes the gap left by layering rules alone. AI-assisted development makes implicit boundaries drift; explicit DTO boundaries keep components stable when domain models change.

### III. Formal testing standard and docs alignment

Automated tests MUST use **xUnit v3** (Microsoft.Testing.Platform), **NSubstitute** for doubles, and **built-in `Assert` only**. **bUnit** is limited to `SoloDevBoard.Web.Tests`; **Playwright** E2E lives in `SoloDevBoard.E2E.Tests`. Moq, FluentAssertions, NUnit, MSTest, and AwesomeAssertions are banned.

Tests MUST follow `MethodUnderTest_Scenario_ExpectedOutcome` naming with Arrange / Act / Assert sections. The distributed Aspire AppHost application graph (resource declarations, references, publish model) MUST NOT be unit-tested. Extracted deterministic deploy and parameter resolution helpers MAY be unit-tested when documented as staying aligned with AppHost (for example the `AppHostDeployParameterResolver` pattern in Infrastructure tests). Fast CI and local paths MUST use `SoloDevBoard.UnitTests.slnf` (ADR-0041); E2E is owned by `playwright.yml` with category filters, not as the default `dotnet test` on the full solution.

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

Every compute project in the AppHost graph MUST call `AddServiceDefaults()` and reference `SoloDevBoard.ServiceDefaults` (see Principle VI). Health checks, structured logging, and OpenTelemetry export MUST remain enabled on hosted and Aspire paths. Logging patterns SHOULD remain compatible with Aspire telemetry where practical, avoiding patterns that surface benign noise as dashboard errors. CI MUST stay split across focused workflows (ADR-0030) rather than one monolithic pipeline.

**Rationale:** Blazor Server multi-repo workloads amplify GitHub latency; caching and async discipline are product requirements, not optimisations of last resort.

### VI. AppHost as the single environment model

**Single graph:** The resource graph, Azure deploy targeting, AppHost parameters, and production deploy automation MUST be declared in `SoloDevBoard.AppHost`. Parallel hand-maintained production infrastructure for the same concerns MUST NOT be reintroduced (ADR-0018 supersedes hand-maintained Bicep for Azure Container Apps).

**Aspire-first local development:** Local development MUST treat `aspire start` via AppHost as the standard path; direct `dotnet run` on Web MAY remain only as a documented fallback (ADR-0016).

**ServiceDefaults:** Every compute project in the AppHost graph MUST call `AddServiceDefaults()` and reference `SoloDevBoard.ServiceDefaults` (Principle V).

**New resources:** Additional Azure resources or compute projects in AppHost MUST pass the proportionality gate (Principle II) and MUST have a **binding** ADR when the change is material (ADR-0016 revisit triggers).

**Aspire-referenced resource access (DI):** For every backing service modelled in AppHost and passed with `WithReference`, consuming hosts (Web, future workers) MUST register access only through the matching Aspire client integration (or an abstraction registered from that integration), using the same resource name as in AppHost. Application and Infrastructure code MUST obtain databases, connection factories, and cloud clients via constructor injection of those registered services (for example `DbDataSource`, EF `DbContext`, or factory patterns where an ADR chooses a store). They MUST NOT construct clients from hand-maintained connection strings, duplicate configuration keys, or ad hoc `IConfiguration` lookups that bypass the Aspire-injected configuration shape. `AddSoloDevBoard` remains the application DI composition root (Principle I); Aspire resource client registration MAY occur in host `Program.cs` before `AddSoloDevBoard`. Test projects MAY use fakes or containers; these MUST rules apply to production host code paths only.

**Wiring:** Hardcoded endpoint URLs MUST NOT be passed in `WithEnvironment` where AppHost endpoint references or `WithReference` apply (align with aspireify-style endpoint expressions).

**Rationale:** One orchestration and deploy surface prevents environment drift; aligns with layered architecture and constructor injection.

### VII. Local-first orchestration and graceful capability degradation

**Minimal friction for solo local use:** A solo developer MUST be able to run the product locally via Aspire (for example `aspire start` or `aspire run` on the AppHost) with only user-facing web compute plus GitHub credentials, without an Azure subscription, without mandatory provisioning of auxiliary AppHost resources (SQL, Azure Functions, storage emulators, scheduled workers, or other full-experience infrastructure), and without a GitHub App. Credentials MAY be supplied via Aspire parameters, environment variables, user secrets, or an optional existing dev Key Vault (ADR-0045); none of these paths MUST force the full resource graph.

**Two orchestration postures, one product:** The product retains the existing mutually exclusive authentication pair documented in `docs/getting-started.md` (PAT-only local trusted mode with `HostedSignInEnabled` false, and hosted sign-in with GitHub App OAuth and operator admission control). A third authentication or deployment mode MUST NOT be introduced.

- **PAT-only local trusted mode:** AppHost models a **lean AppHost graph**: the web app, and optionally Key Vault bindings for secrets only. This is the default path for contributors, dogfooding, and trusted personal use (including localhost and private networks).
- **Hosted sign-in posture:** When hosted sign-in and production-style deployment are in use (including a future commercial multi-tenant hosted offering), AppHost models **`web` compute** plus auth and admission today. AppHost MAY later model additional resources for scheduled work, persistence, and background processing only when a **binding** ADR adds them to the graph.
- **Trusted personal deployment:** A solo operator MAY deploy to their own Azure subscription while remaining in PAT-only local trusted mode (`docs/deployment.md` self-hoster path). That deployment MUST NOT be treated as permission to expose a PAT-backed instance on an untrusted public multi-user endpoint; admission control and GitHub App sign-in remain the path for shared or public hosting. Trusted personal Azure deployments default to the lean AppHost graph unless a future binding ADR and operator opt-in add more resources.

Governance text MUST use **PAT-only local trusted mode**, **hosted sign-in**, **lean AppHost graph**, and **full experience posture** consistently. Product modes named “simple mode”, “PAT mode”, or “hosted mode” MUST NOT appear in governance text.

**Feature parity through graceful degradation:** Every user-facing capability in the product’s in-scope basic feature set MUST remain reachable in PAT-only local trusted mode with the lean AppHost graph. Features that depend on background infrastructure MUST still be usable in lean posture via on-demand, in-process, or otherwise resource-light behaviour where technically feasible (for example an inventory-style scanner may run on demand in lean posture; scheduled overnight scans and durable snapshot stores require optional AppHost resources only when a binding ADR adds them). The UI and Application layer MUST NOT present blocking copy such as “not available in PAT-only mode”, “not available in PAT mode”, or equivalent hard denial for in-scope features. Where behaviour differs because infrastructure is absent, the product MUST explain the difference in capability (for example no schedule, no cross-session snapshot history) and MUST offer the best available alternative in lean posture. Spec Kit feature specs and tasks MUST call out lean-posture behaviour whenever a feature adds AppHost resources or scheduled work.

**Commercial hosted offering (directional):** The product MAY evolve a commercial hosted service with subscriptions on top of the hosted sign-in full experience posture. That MUST NOT remove or erode the lean PAT-only path for open-source contributors and solo developers running locally.

**Security boundary:** PAT-only local trusted mode provides no multi-user isolation. Constitutional emphasis is minimal resources and maximal feature reach in **trusted** lean use, not encouraging PAT credentials on shared public URLs.

**Rationale:** Solo contributors and open-source use stay low-friction; hosted and commercial paths scale orchestration without forking the product into incompatible modes; graceful degradation keeps the in-scope feature set honest without hard gates (ADR-0018, ADR-0045).

## Platform and Security Constraints

- **Stack (fixed for v1.x):** .NET 10 LTS (`net10.0`), C# 14, Blazor Server, MudBlazor, Aspire AppHost for local orchestration and Azure Container Apps deployment (ADR-0001, ADR-0012, ADR-0018). Optional Azure resources (Functions, databases, storage, and similar) belong in the graph only when a binding ADR models them. Replacing Blazor Server with a SPA stack requires a major ADR and scope revision (GOALS kill criteria).
- **Quality bar on build:** Nullable reference types and implicit usings enabled; `dotnet format SoloDevBoard.slnx --verify-no-changes` and `dotnet build SoloDevBoard.slnx -c Release -warnaserror` MUST pass before merge-quality work ships.
- **Secrets and parameters:** GitHub and auth configuration for runnable environments MUST flow through AppHost parameters and `WithEnvironment` into apps (`GitHubAuth__*`, sentinel placeholders per existing app conventions). Credentials MUST NOT be committed. Tier deploy auth secrets follow ADR-0045 (persistent Key Vault, `PublishAsExisting`; routine CD MUST NOT revive passing tier `gh-pat` / `gh-app-client-secret` via GitHub `Parameters__*` after migration). The application MUST read secrets from configuration; in-app Key Vault clients MUST NOT be added unless a new ADR says otherwise (ADR-0045). Runtime secrets MAY also use Aspire user secrets or documented operator paths (ADR-0043). GitHub Actions CD MUST use OIDC for Azure, not long-lived cloud keys in source control.
- **Toolchain alignment:** AppHost `Aspire.Hosting.AppHost`, other `Aspire.Hosting.*` packages, and the documented Aspire CLI version MUST stay on a single release family when upgraded (for example the 17.0.0 family per `docs/getting-started.md` and `plan/CURSOR_CLOUD.md`).
- **Hosted access:** Public deployments MUST enforce operator-managed admission control (deny-by-default) before repository data loads (ADR-0014).
- **Documentation language:** UK English in user-facing strings, comments, and docs; the Unicode em dash (U+2014) MUST NOT appear in repository prose.
- **Decisions:** Significant architecture or technology choices MUST be recorded under `adr/`; rejected options in archive or legacy logs do not override active ADRs.
- **Out of product scope unless replanned:** Non-GitHub providers, team collaboration features, native mobile clients, marketplace monetisation in v1.0 (see `SCOPE.md` / `plan/SCOPE.md`). Operator-hosted commercial SaaS on Azure is an allowed future direction and is not a kill criterion.

## Delivery Workflow and Quality Gates

- **Spec Kit ownership:** Feature intent for new work flows through Spec Kit artefacts (`spec.md`, `plan.md`, `tasks.md` under `specs/<feature>/`). Those artefacts MUST NOT contradict this constitution. Detailed day-to-day coding rules remain in `AGENTS.md` and `CONVENTIONS.md` and MUST be kept consistent with amended principles.
- **AppHost documentation sync:** New or changed AppHost parameters, auth modes, resource references, or deploy steps that affect operators or developers MUST update `docs/getting-started.md`, `docs/deployment.md`, and `src/SoloDevBoard.AppHost/README.md` in the same change when behaviour is operator-facing (mirroring `CONVENTIONS.md` documentation sync).
- **Pull requests:** MUST follow `plan/PULL_REQUEST_POLICY.md` (title shape, template body, labels). Agent-authored PRs MUST receive human review before merge.
- **Pre-merge verification (minimum):** `dotnet format` verify, Release build with warnings as errors, `dotnet test SoloDevBoard.UnitTests.slnf -c Release --no-build` when code or tests change; Playwright-impacting UI changes MUST consider `playwright.yml` tiers and journey docs.
- **Historical artefacts:** Material under `plan/archive/` (Copilot-era bundles, weekly PM notes, retired implementation plans) MAY inform context but MUST NOT be mandated as ongoing process. Open work lives in GitHub Issues and Project #8, not new archive bundles.
- **Agent skills:** Project skills in `.agents/skills/` (e.g. `implementation-guardrails`, `mudblazor`, `code-review`) and Aspire workflow skills from the Cursor aspire-skills plugin (`.cursor/settings.json`) supplement the constitution during implementation; they do not replace ADRs or quality gates. Aspireify ceremony, aspire-orchestration CLI troubleshooting, and Playwright port differences remain operational detail in skills and docs, not constitution text.

## Governance

This constitution is the **non-negotiable governance layer** for Spec Kit-driven feature work on SoloDevBoard. It codifies brownfield practice distilled from `GOALS.md`, `CONVENTIONS.md`, `adr/`, CI workflows, E2E journey definitions, and archived planning history (without requiring that history's workflows going forward).

**Hierarchy when guidance conflicts:**

1. This constitution (principles and MUST rules).
2. Active ADRs in `adr/` for the specific decision domain.
3. `AGENTS.md`, `CONVENTIONS.md`, and `plan/PULL_REQUEST_POLICY.md` for operational detail.
4. Per-feature Spec Kit specs and plans (MUST comply with 1–3).

**Amendments:** Changes MUST go through `/speckit-constitution`, bump `CONSTITUTION_VERSION` using semantic versioning (MAJOR for removed or redefined principles; MINOR for new principles or material expansions; PATCH for clarifications only), set `LAST_AMENDED_DATE` to the amendment date, and include a Sync Impact Report comment until the amended file is committed.

**Compliance review:** Authors and reviewers MUST verify feature specs, tasks, and PRs against relevant principles. Complexity beyond existing patterns MUST be justified in the spec or an ADR. Constitution violations MUST be fixed or explicitly escalated for amendment, not silently ignored.

**Runtime guidance:** Use `AGENTS.md` and `CONVENTIONS.md` for command sequences, file placement, and documentation sync tables; use `adr/` for decision history; use project skills in `.agents/skills/` and Aspire plugin skills per `AGENTS.md` during implementation.

**Version**: 1.1.2 | **Ratified**: 2026-11-15 | **Last Amended**: 2026-11-20