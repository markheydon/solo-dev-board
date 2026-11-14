# Implementation guardrails (detail)

Use with [../SKILL.md](../SKILL.md). [checklists.md](checklists.md) has proportionality red flags, suppression inventory, EF tree, and pre-PR checklist.

## Proportionality gate

Do **not** add projects, layers, interfaces, MediatR/CQRS ceremony, generic repositories, factories, coordinators, or extra DTO mappings by default.

Answer before new structure:

1. What concrete problem does this solve **today**?
2. What boundary does it enforce?
3. What coupling does it remove?
4. What would materially worsen if it did not exist?
5. Is there more than one meaningful implementation or consumer?
6. Is independent deployment, reuse, security, ownership, testing, or replacement actually required?
7. Does maintenance and cognitive cost exceed current benefit?

**Prefer** deletion and consolidation when an abstraction has no demonstrated value. Do not remove boundaries justified by security, domain ownership, deployment, dependency control, testability, provider variability, public API stability, or documented near-term requirements.

## Forbidden default fixes

Unless the user explicitly approves a **narrow, documented, justified** exception:

- `NoWarn`, weakened `AnalysisLevel` / `AnalysisMode`, `EnableNETAnalyzers=false`.
- Nullable disable or broad `#nullable disable`.
- `#pragma warning disable`, `SuppressMessage`, silent `.editorconfig` severity reductions.
- Weakening `TreatWarningsAsErrors` for Release or CI.
- Disabling, skipping, or filtering tests to pass.
- Weakening assertions, removing exception assertions, or matching faulty production behaviour in tests.
- Empty catch blocks, swallowed exceptions, `default!`, null-forgiving to silence analysis.
- Broad `dynamic` or reflection to bypass type safety.
- "Temporary" HACK without removal criteria.

Legitimate suppressions: exact diagnostic ID, root cause understood, minimal scope, rationale (comment or ADR).

## Agent-shaped defects

- Duplicate implementations instead of extending existing code.
- Parallel abstractions for the same concept; code in the wrong layer.
- Helper types that obscure ownership; implementation details leaking across boundaries.
- Sync-over-async, ignored cancellation, retries without limits/jitter/idempotency analysis.
- Hard-coded values bypassing configuration; validation only in UI.
- Tests changed to accept incorrect behaviour; docs updated to legitimise a workaround.
- Large refactors for tiny problems; speculative "future-proof" abstractions.
- New dependencies or feature flags without clear requirement.
- Locally correct edits that break global consistency.

## EF Core (summary)

- Purposeful DbContext boundaries; no thin repository wrappers without a real boundary.
- Explicit queries, tracking, transactions, concurrency at the right layer.
- Do not leak EF types into layers that should not own persistence.
- Do not rewrite **applied** production migrations without explicit user strategy.

## Preserve

Do not simplify away:

- Boundaries backed by ADRs, security, or deployment facts.
- Warnings-as-errors and strong analyzer configuration.
- Narrow, documented suppressions.
- Production EF migration history that must stay immutable.
- Tests that guard regressions or architectural rules.

## Before submitting

> If a very experienced engineer inherited this repo tomorrow, would my change make them stop and investigate?

If yes, rework before submit.
