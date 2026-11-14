---
name: principal-engineering
description: >-
  Applies principal-engineer guardrails during implementation so changes match
  documented architecture, stay proportionate, fix root causes, and preserve
  diagnostic and test integrity. Use when implementing features, refactors, EF
  migrations, build or analyzer fixes, new projects or abstractions, or any
  non-trivial code change in a .NET repository—especially work done by coding
  agents. Not for one-off repository audits or post-hoc PR review (use
  code-review instead).
---

# Principal engineering guardrails

Think like a principal engineer **while building**, not only when auditing later.
A green build or passing tests is necessary; it is not proof the change is sound.

## When to use

Apply this skill for non-trivial implementation work. Skip for trivial edits (typos,
comments only) unless they touch build, analyzer, or CI configuration.

| Situation | Apply |
| --- | --- |
| Feature, bug fix, refactor spanning layers | Yes |
| Fixing CS/analyzer/build/test failures | Yes — root-cause discipline is critical |
| New project, interface, layer, NuGet package | Yes — proportionality gate |
| EF entities, migrations, DbContext | Yes |
| Blazor/UI-only change with no architecture impact | Light pass (layers, validation ownership) |
| User asked for a full repository audit | No — use the audit prompt; do not produce audit report sections here |

Complements [code-review](../code-review/SKILL.md): that skill reviews diffs before merge;
this skill prevents the defects that review would flag.

## 1. Read constraints before writing code

Read applicable repository instructions **before** judging or changing the system:

- Agent instructions (`AGENTS.md`, copilot/agent rule files)
- Constitution and non-negotiable mandates
- `CONVENTIONS.md`, active feature spec under `specs/`
- ADRs and architecture docs
- Build centralisation: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, CI workflows

Treat documented architectural decisions as **constraints** unless the user explicitly
authorises challenging them with evidence. If implementation contradicts docs, fix the
 mismatch or update docs/ADR—do not silently diverge.

Discover product intent from docs and code; do not invent requirements.

## 2. Place work in the real architecture

Verify boundaries through evidence, not folder or project names:

- Project and package references, public/internal visibility
- Composition roots and DI registration
- Data flow, persistence access, external integrations

Before adding or moving code, confirm:

- Which component **owns** the business rule or invariant
- Dependency direction (domain must not depend on presentation/infrastructure details)
- Whether the change belongs in the **smallest** existing home that preserves boundaries

Do not expand public surface area, configuration knobs, or integration contracts without
a demonstrated need.

## 3. Proportionality gate (mandatory before new structure)

Do **not** add projects, layers, interfaces, MediatR/CQRS ceremony, generic repositories,
factories, coordinators, or extra DTO mappings by default.

Before introducing structural complexity, answer honestly:

1. What concrete problem does this solve **today**?
2. What boundary does it enforce?
3. What coupling does it remove?
4. What would materially worsen if it did not exist?
5. Is there more than one meaningful implementation or consumer?
6. Is independent deployment, reuse, security, ownership, testing, or replacement actually required?
7. Does maintenance and cognitive cost exceed current benefit?

**Prefer** deletion and consolidation when an abstraction has no demonstrated value.
**Do not** remove boundaries justified by security, domain ownership, deployment,
dependency control, testability, provider variability, public API stability, or
documented near-term requirements.

Do not swap one elaborate architecture for another without a demonstrated problem.

For extended examples (ceremonial Clean Architecture, one-interface wrappers), see
[checklists.md](checklists.md).

## 4. Root cause over symptoms

Fix the underlying issue. Do not make diagnostics or tests “go green” by hiding failure.

### Forbidden default fixes

Unless the user explicitly approves a **narrow, documented, justified** exception:

- `NoWarn`, weakened `AnalysisLevel` / `AnalysisMode`, `EnableNETAnalyzers=false`
- Nullable disable or broad `#nullable disable`
- `#pragma warning disable`, `SuppressMessage`, silent `.editorconfig` severity reductions
- Weakening `TreatWarningsAsErrors` for Release or CI
- Disabling, skipping, or filtering tests to pass
- Weakening assertions, removing exception assertions, or matching faulty production behaviour in tests
- Empty catch blocks, swallowed exceptions, `default!`, null-forgiving to silence analysis
- Broad `dynamic` or reflection to bypass type safety
- “Temporary” HACK without removal criteria

A successful build only proves the current configuration allows compilation—not that
warnings, nullability, architecture, or tests were handled correctly.

Legitimate suppressions require: exact diagnostic ID, root cause understood, minimal scope,
and rationale (comment or ADR). Prefer **narrowing** over blanket suppression.

Search patterns and classification: [checklists.md](checklists.md#diagnostic-integrity).

## 5. Modern C# and .NET (repository-aligned)

Use the repo’s pinned SDK and target frameworks as source of truth (`global.json`, project files).

- Enable and respect nullable reference types; propagate `CancellationToken` when the API supports it
- Use modern BCL/APIs when they improve correctness, safety, clarity, or maintainability—not for novelty
- Do not recommend or adopt new language features without a concrete benefit
- Do not add NuGet packages when the platform or an existing dependency already covers the need
- Follow repository package rules (e.g. central package management, explicit user approval to add packages)

## 6. Agent-shaped defects (stop and reconsider)

Be alert to changes that satisfy literal task acceptance while violating system intent:

- Duplicate implementations instead of extending existing code
- Parallel abstractions for the same concept; code in the wrong layer
- Helper types that obscure ownership; implementation details leaking across boundaries
- Sync-over-async, ignored cancellation, retries without limits/jitter/idempotency analysis
- Hard-coded values bypassing configuration; validation only in UI
- Tests changed to accept incorrect behaviour; docs updated to legitimise a workaround
- Large refactors for tiny problems; speculative “future-proof” abstractions
- New dependencies or feature flags without clear requirement
- Locally correct edits that break global consistency

If the fix feels like “make the agent/task pass”, pause and apply sections 2–4 again.

## 7. Entity Framework Core and migrations

When touching persistence:

- Keep DbContext boundaries purposeful; avoid repository/unit-of-work wrappers that only forward `DbSet` unless they enforce a real boundary (domain, security, testing, provider)
- Keep queries, tracking, transactions, and concurrency explicit at the right layer
- Do not leak EF types into layers that should not own persistence

**Migration hygiene**

- Determine lifecycle: pre-release disposable DB vs deployed history that must be preserved
- Do not rewrite, squash, or reorder **applied** production migrations without explicit user strategy and understanding of data impact
- Pre-release churn (fix/rename cycles) may warrant consolidation **only** when databases are safely recreatable and the user agrees
- Prefer one coherent baseline before first release over accumulated trial-and-error migrations

Decision tree: [checklists.md](checklists.md#ef-core-migrations).

## 8. Testing discipline

Tests must protect **behaviour** and important boundaries—not implementation trivia.

- Add or update tests when behaviour changes; do not over-mock internal details
- Do not disable or skip tests to merge; fix or isolate flakiness properly
- Prefer precise assertions; failure paths and cancellation matter when relevant
- Distinguish “no test yet for unbuilt feature” from “test weakened to match a bug”

## 9. Security and operations (proportionate)

- Enforce authz and validation at the correct trust boundary—not UI only
- Do not log secrets or sensitive payloads
- Use structured logging, health checks, and observability appropriate to the app’s maturity
- Ask: if this fails in production at 03:00, is there enough signal to diagnose it?

Do not bolt on enterprise observability for a trivial change without demonstrated benefit.

## 10. Finish strong

Before claiming work is complete:

1. Run repository validation commands (format, build with warnings as errors, tests)—see `AGENTS.md`
2. Scan your diff for forbidden default fixes (section 4) and agent-shaped defects (section 6)
3. Confirm complexity added passes the proportionality gate (section 3)
4. Ensure changes align with documented architecture or documented updates you made

### Escalate to the human when

- Production migration history or data compatibility is unclear
- Challenge to an ADR or constitution mandate is required
- Product/compatibility/deployment authority is needed
- Security or data-loss risk is credible

Provide a safe default only when the user cannot answer immediately—not to avoid engineering judgement.

## Preserve (do not “fix” away)

Do not simplify away items that are intentionally correct:

- Boundaries backed by ADRs, security, or deployment facts
- Warnings-as-errors and strong analyzer configuration
- Narrow, documented suppressions (generated code, platform quirks, confirmed analyzer bugs)
- Production EF migration history that must remain immutable
- Tests that look verbose but guard regressions or architectural rules

## Behavioural rules (summary)

- Prefer root-cause fixes; prefer simple designs that meet **actual** requirements
- Do not equate passing build/tests with correct architecture or good testing
- Do not equate more projects or abstractions with better design
- Do not weaken CI or quality gates to accommodate new code—raise code to the gate
- Evidence over hypothesis when unsure; investigate callers and run targeted tests
- Minimal diff that solves the real problem; match existing conventions

Ask before finishing:

> If a very experienced engineer inherited this repo tomorrow, would my change make them stop and investigate?

If yes, rework before submit.

## Additional reference

- [checklists.md](checklists.md) — proportionality red flags, suppression inventory, EF migration tree, pre-PR self-review
