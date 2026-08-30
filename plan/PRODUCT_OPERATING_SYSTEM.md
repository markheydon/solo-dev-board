# Product operating system — deferred direction

**Status:** Direction only. Not in scope for `v1.1`. Unmilestoned; ice-boxed as [#475](https://github.com/markheydon/solo-dev-board/issues/475).  
**Does not replace:** PAT-only local trusted mode, or the existing GitHub operations features.  
**Detail:** Field lists, examples, positioning, and the 2026-08-30 discussion notes are in [Captured vision (2026-08-30)](#captured-vision-2026-08-30). That appendix is memory, not committed scope.

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

Suggested first delivery slice when that happens (not committed): **Product** (name, status, intent, related repos, key links) plus a **Journal** attached to it. Ideas and Feedback come after a Product id exists. Do not start with a generic CRM or research wiki.

---

## Captured vision (2026-08-30)

The sections above are the contract for later planning. This appendix keeps the original thinking so it is still recoverable if delivery is months or a year away. **It is not a backlog, not a spec, and not a DEC.** Field names and examples may change when this is planned properly.

### Why this exists

Today SoloDevBoard is positioned as a single pane of glass for solo developers managing GitHub workloads across multiple repositories.

GitHub’s tree starts at the repository:

```
GitHub
 ├─ Repositories
 ├─ Issues
 ├─ PRs
 └─ Projects
```

Solo developers actually think in products:

```
Business / Products
 ├─ Product ideas
 ├─ Validation
 ├─ Customer feedback
 ├─ Roadmap
 ├─ Features
 ├─ Repositories
 ├─ Issues
 └─ Releases
```

GitHub starts at the repository. The developer starts at the product. That is the gap.

There are plenty of GitHub dashboards, git analytics tools, and repository managers. There are few tools aimed at: “I am a solo developer running multiple products and need one place to think.” If the direction holds, SoloDevBoard is closer to a **product operating system for solo developers** than to a GitHub administration tool. GitHub remains integral as delivery, not an afterthought.

### Motivating scatter (personal)

Work already spans several products, for example:

- SoloDevBoard
- Import To Planner
- BillDrift
- FreeAgent integrations
- Meaty Times
- Various experiments

Notes for those products are split across GitHub issues, GitHub Discussions, OneNote, Markdown files, chat threads, ADRs, and planning docs. The moment a product spans multiple repos (application, documentation, website, automation), GitHub is awkward as the top-level organiser.

The fact that this direction itself had nowhere to live except a Markdown file and an ice-box epic is evidence that an incubator for pre-repo ideas is useful.

### Product catalogue

Think of a portfolio:

```
Products
 ├─ SoloDevBoard
 ├─ Import To Planner
 ├─ BillDrift
 └─ New SaaS idea
```

Candidate fields per product:

- Description
- Status (`Idea`, `Validating`, `Building`, `Active`, `Maintenance`, `Archived`, plus cancelled/abandoned)
- Revenue model
- Target audience
- Related repositories
- Related websites
- Key links

Analogy at capture time: Azure DevOps area paths mixed with a portfolio view. Related repos are pointers into the existing GitHub catalogue, not a second repo list.

### Idea incubator

Capture ideas **before** they become repos. Most ideas never need a GitHub repository.

Candidate fields:

- Problem statement
- Proposed solution
- Audience
- Evidence
- Competitors
- Confidence score

An idea with zero repos is a lifecycle stage. Expected end states are cancelled/abandoned, or one or more linked repos once Building.

### Product journal

The capability most solo developers lack. Instead of OneNote (or equivalent), a chronological log on the product.

Example entry:

> **2026-08-30**  
> Had a discussion about a customer feedback widget. Thinking GitHub Discussions is not enough. Need lightweight voting.

Blend of working notes, engineering journal, and founder diary. Not a substitute for GitHub issue comments on delivery work.

### Feedback registry

A lightweight register of product feedback, not a general CRM.

Candidate fields:

- Source
- Customer
- Date
- Product
- Theme
- Status

Example: three customers requested CSV import, email notifications, and SSO. Over time, recurring demand should be visible without mining chat logs.

### Product roadmaps

Not independent roadmaps that compete with GitHub Projects. Nesting at capture time:

```
Product
   → Feature
      → GitHub Epic
         → GitHub Issues
```

SoloDevBoard is planning. GitHub is delivery. Cross-Repo Planning already hangs off a Projects v2 board; this overlay would hang that board (and repos) off a Product.

### Research and discovery

Store competitor research, screenshots, links, and market notes as a lightweight product wiki. Much of this already exists scattered across Markdown and OneNote.

### What not to build

Do not add:

- Sprint planning
- Story points
- Velocity charts
- Burn-downs
- Agile ceremonies
- Team workflows

The failure mode is accidentally building “Jira for one person”. Nobody wants that. Collaboration and team features remain out of SCOPE for the current product.

### Auth and data (from the 2026-08-30 discussion)

Keep **two planes**:

| Plane | Job | Credential | Persistence |
|------|-----|------------|-------------|
| Operations | Labels, triage, migration, audit, Actions, GitHub Planning | PAT or hosted GitHub App | GitHub plus a little `localStorage` |
| Product OS | Catalogue, ideas, journal, feedback, research, product roadmaps | Same GitHub identity (PAT login or hosted user) | Application-owned store (not chosen yet) |

Do not tie Product OS to hosted sign-in only. Do not require a product record before Label Manager (or any operations page) works. Do not put journal or feedback in `localStorage`.

Repository groups ([#381](https://github.com/markheydon/solo-dev-board/issues/381)) are a half-step toward grouping repos. They must not be silently renamed into products. A product may exist with no repos; a repo group cannot.
