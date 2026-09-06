# Technical Debt Review

**Runbook:** [`plan/PM_RUNBOOK.md`](../../plan/PM_RUNBOOK.md) — Technical Debt Review
**Issue template (follow-up only):** [`.github/ISSUE_TEMPLATE/chore.yml`](../../.github/ISSUE_TEMPLATE/chore.yml)

## Purpose

On-demand, read-only deep dive of the codebase to surface **candidate technical debt** for a human to review. Typical cadence is monthly. This is not a daily hunt, not a refactor, and not an issue-filing bot.

A candidate is a **repeated or spreading gap against a written SoloDevBoard standard**, not “code an agent would have written differently”.

## Easy-to-miss specifics

- **Read-only by default.** Do not modify application code, open a pull request, or create GitHub issues unless the user explicitly asks **after** reviewing the report.
- **Do not confuse this with a bug hunt.** Correctness bugs that would crash, lose data, or bypass auth belong in a bug report, not this list.
- **Prefer clusters.** One odd file is usually not worth a chore issue. The same wrong pattern in several places is.
- **Cap the list.** Report at most **ten** candidates, strongest first. If you find more, merge them by pattern rather than listing every file.
- **Deduplicate.** Do not re-report a cluster that is already an open `type/chore` (or related) issue, an accepted trade-off in `plan/DECISIONS.md`, or a finding in a previous `plan/debt-reviews/` artefact that the user declined.
- Persist the artefact under `plan/debt-reviews/YYYY-MM-DD.md` so the next run has a baseline. Do not write other temporary files.

## Significance bar

Include a candidate only when you can state all of the following:

1. **Standard** — which written rule it violates (`AGENTS.md`, a DEC, MudBlazor-first UI, DTO boundary, test/docs alignment, and so on).
2. **Evidence** — concrete paths or a counted cluster (not a vibe).
3. **Cost** — why the next change will pay interest (copy-paste of the wrong pattern, missing tests for shipped behaviour, layer leak, and so on).
4. **Suggested unit of work** — one chore-sized slice, or “too large: file an issue only”.

If any of those four are missing, omit it. Style nits, single unused usings, speculative rewrites, and “nice to have” naming changes are out of scope.

## Procedure

### 1. Load standards and prior findings

Read:

- [`AGENTS.md`](../../AGENTS.md) — architecture, UK English, testing, documentation sync.
- [`plan/DECISIONS.md`](../../plan/DECISIONS.md) — accepted trade-offs; do not treat those as debt.
- [`plan/LABEL_STRATEGY.md`](../../plan/LABEL_STRATEGY.md) — `type/chore` is the default follow-up type.
- Newest file in `plan/debt-reviews/` if the directory exists.

Then gather open tracking so you do not duplicate it:

- `gh issue list --label type/chore --state open`
- Broader search for open issues whose titles already name the same cluster (refactor, MudBlazor, boundary, duplication).

### 2. Scan by lens (deep, not grep-and-dump)

Cover each lens with enough code-path tracing to confirm a cluster. Parallel exploration is fine; the report must still be one ranked list.

| Lens | What to look for |
|------|------------------|
| **Layer boundaries** | App referencing Infrastructure; domain entities on public Application `I*Service` / `I*Manager` signatures; business logic in `.razor` files. |
| **MudBlazor-first UI** | Repeated raw HTML or custom CSS where a MudBlazor component, parameter, or utility class already exists. |
| **GitHub / API mapping** | Duplicated HTTP or GraphQL translation that should live in Infrastructure; Application leaking transport types. |
| **Docs vs tests** | User-guide claims in `website/content/docs/` with no matching Playwright spec (or the reverse); shipped behaviour with no bUnit/xUnit lock-in. |
| **Decision drift** | Code that contradicts a DEC without a newer decision. |
| **Testing standard** | FluentAssertions, Moq, Shouldly, NUnit, MSTest, or other banned libraries; tests that do not follow `MethodUnderTest_Scenario_ExpectedOutcome`. |
| **Composition / hosting** | Hand-authored production Bicep; AppHost secrets in source; App bypassing `AddSoloDevBoard`. |
| **Stale markers** | `TODO` / `FIXME` / `HACK` clusters that have aged without an issue. |
| **UK English (clustered)** | Repeated US spelling in user-facing strings or comments, not a single typo. |

Ignore formatting, personal style, planned-but-unbuilt features, and one-off placeholders that are clearly scoped to an in-flight issue.

### 3. Rank and draft

For each surviving candidate, assign:

| Rank | Meaning |
|------|---------|
| **High** | Clustered, will spread, not tracked, chore-sized or clearly issue-worthy. |
| **Medium** | Real cost but localised, partly mitigated, or needs a product/design call before coding. |
| **Low** | Do not list. |

Draft a **suggested issue title** in policy shape (`[Chore] …`) and a short definition of done. Do not create the issue yet.

### 4. Write the artefact

Save to `plan/debt-reviews/YYYY-MM-DD.md` (today's date, ISO format). Create the directory if needed.

**Title:** `Technical Debt Review — {D MMMM YYYY}` (UK English date).

```markdown
# Technical Debt Review — {date}

**Cadence:** On-demand (typically monthly).
**Scope:** Whole codebase against written SoloDevBoard standards.
**Action taken:** Report only. No issues or pull requests were opened.

## Summary

{One paragraph: how many candidates, strongest theme, anything explicitly deferred as accepted trade-off.}

## Already tracked (excluded)

- {issue or DEC reference} — {one-line reason it is not a new candidate}.

## Candidates (human review)

### {N}. {short name} — {High | Medium}

- **Standard:** {AGENTS.md / DEC-NNN / other}.
- **Evidence:** {paths, counts, or representative snippets described in prose}.
- **Cost if ignored:** {copy-paste risk or maintenance cost}.
- **Suggested unit of work:** {one PR-sized slice, or "issue only — repo-wide"}.
- **Suggested issue title:** `[Chore] …`
- **Definition of done:** {bullets}.

## Not listed

{Optional: one short note on classes of smell you saw and discarded, so the next run does not re-litigate them.}
```

### 5. Present to the user

In chat, lead with the ranked candidates (names, rank, one-line cost). Point to the saved artefact. Ask which items, if any, should become GitHub issues.

Only if the user names items to file:

- Create issues from [`.github/ISSUE_TEMPLATE/chore.yml`](../../.github/ISSUE_TEMPLATE/chore.yml) via the `repo-github-issues` skill.
- Apply `type/chore` plus a `priority/` label (default `priority/low` unless the user says otherwise).
- Sync to Project #8 when that skill requires it.
- Do not implement the chores in the same session unless the user asks.

## Invocation

**Chat:** "Run a technical debt review" or "Monthly technical debt deep dive".
**Slash command:** `/technical-debt-review`.
**GitHub Issue comment** (mention at the start; repo-wide scan, not limited to this issue):
- `@cursor run a technical debt review`
- `@cursor monthly debt deep dive`

Do not treat this as "implement this issue" or as a request to open chores without a human pick.
