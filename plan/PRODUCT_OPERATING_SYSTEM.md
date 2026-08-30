# Product operating system — deferred direction

**Status:** Direction only. Not in scope for `v1.1`. Unmilestoned; ice-boxed as [#475](https://github.com/markheydon/solo-dev-board/issues/475).  
**Does not replace:** PAT-only local trusted mode, or the existing GitHub operations features.

Solo developers think in **products**. GitHub starts at **repositories**. This note records a future overlay that closes that gap without turning SoloDevBoard into Jira for one person.

## Intent

Keep GitHub as the delivery spine (repositories, issues, pull requests, Projects, releases). Add a **product** layer above it so ideas, validation, feedback, research, and a journal have a home **before** (and beside) repos.

This is not a new authentication mode and not a replacement for Label Manager, Triage, Migration, Audit, Actions Templates, or Cross-Repo Planning.

## Lifecycle

A product (or idea) may exist with **no linked repository**. That is expected for early capture.

Expected outcomes:

- **Cancelled** or **abandoned** — never gained a repo; the record remains for the journal and to avoid repeating dead ends.
- **Building / Active / Maintenance** — one or more linked GitHub repositories. GitHub is integral, not an afterthought.

Suggested status set (names can change at delivery time):

`Idea` → `Validating` → `Building` → `Active` → `Maintenance` → `Archived`  
plus `Cancelled` / `Abandoned` as terminal states that never required a repo.

Invariant to preserve when this is planned: **operations pages must keep working with PAT mode and with no product records.** Product is a lens, not a gate.

## Shape (not a backlog)

```
Portfolio
 └─ Product
      ├─ Ideas (incubator fields; may have zero repos)
      ├─ Feedback
      ├─ Research
      ├─ Journal
      ├─ Roadmap (planning) → GitHub epics / issues (delivery)
      ├─ Repositories (0..n; expected ≥1 once Building)
      └─ Releases
```

Candidate capabilities (do not split into issues until a future release is planned):

- Product catalogue (description, status, audience, revenue model, links, related repos).
- Idea incubator (problem, solution, evidence, competitors, confidence) — most ideas never need a repo.
- Product journal (chronological working notes / founder diary / decision log).
- Feedback registry (source, customer, date, product, theme, status) — not a general CRM.
- Product roadmaps that hang off the product and **delegate delivery to GitHub**.
- Research notes (competitors, links, screenshots) as a lightweight wiki.

## Explicitly out of this direction

Sprint planning, story points, velocity, burn-downs, agile ceremonies, and team workflows. Do not build “Jira for one person”.

## Open questions (no DEC yet)

- Application-owned persistence ([DEC-029](DECISIONS.md#dec-029-cross-repo-planning-board-selection-and-local-settings) currently uses `localStorage` because there is no database). A journal and feedback registry cannot live there.
- How PAT-mode self-hosting provisions that store without dropping the solo operations path.
- Whether “Idea” is a Product in `Idea` status or a distinct type.
- Relationship to repository groups ([#381](https://github.com/markheydon/solo-dev-board/issues/381)) — groups must not silently become products.

## When to promote this

After the current GitHub-operations roadmap (`v1.1`, then whatever is declared next) is the focus. Promote only via the normal planning workflow: GitHub Issues, Project #8, wireframes before page UI, and a decision-log entry when persistence and PAT coexistence are actually chosen.
