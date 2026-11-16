# Open Source Page Wireframe

## Purpose

- Give a single **open-source hygiene** destination in main navigation, starting with **funding file hygiene** for catalogue repositories tagged `open-source` ([#439](https://github.com/markheydon/solo-dev-board/issues/439), [spec US4](../../specs/001-oss-funding-hygiene/spec.md)).
- Read **stored inventory snapshots only**; opening the page must not walk GitHub for every repository.
- Leave catalogue classification on the **Repositories** page; this page does not reclassify topics or manage groups.
- Reserve the route for later hygiene sections (for example community-file readiness) without stubbing empty panels for work that is not built yet.

## User goals

- See at a glance which open-source repositories are missing `FUNDING.yml`, match the saved standard, or differ from it.
- Understand when the last inventory finished, whether it was scheduled or manual, and whether results are fresh, stale, partial, or based on a failed latest attempt.
- Request **Scan now** and keep using the page while the walk runs elsewhere.
- Maintain one **canonical funding file** in the product and know when visible rows were compared against an older standard.
- Open any listed repository on GitHub from its row.
- Distinguish archived, fork, and private traits and exclude archived repositories from **needs attention** counts.

## Layout (desktop)

Follow [product UI language](product-ui-language-wireframe.md): app bar and nav unchanged; content uses page title, purpose line, `MudToolBar`, summary region, then primary table inside `MudPaper`.

```
+--------------------------------------------------------------+
| MudAppBar  [menu]  SoloDevBoard  [spacer]  [theme] [overflow] |
+----------+---------------------------------------------------+
| Nav      | h5  Open source                                   |
| (incl.   | body2  Funding file hygiene for linked open-source  |
|  Open    |        repositories. Reads the latest inventory;    |
|  source) |        it does not scan GitHub while you browse.     |
|          | MudToolBar  [Scan now]  [Edit standard]  [Search]   |
|          | Run metadata (body2): Last finished 16 Nov 2026,    |
|          |   02:14 UTC · Scheduled · Completed · PAT · Fresh   |
|          | [optional MudAlert banners - one primary outcome]    |
|          | Summary chips (MudChipSet):                          |
|          |   Matches 12 | Differs 3 | Missing 4 | Needs attn 7|
|          |   Not scanned 1 | Not visible 2 | Not yet 0 | ...    |
|          | Section: Funding files (only section in v1)           |
|          | MudDataGrid / table:                                 |
|          | +----------------+----------+-----------+---------+ |
|          | | Repository     | Status   | File path | Actions | |
|          | +----------------+----------+-----------+---------+ |
|          | | org/repo-a     | Matches  | .github/… | [GitHub]| |
|          | | org/repo-b     | Missing  | -         | [GitHub]| |
|          | | org/repo-c     | Differs  | FUNDING…  | [GitHub]| |
|          | | org/archived   | Missing  | -         | [GitHub]| |
|          | |   (Archived)   |          |           |         | |
|          +----------------+----------+-----------+---------+ |
|          | Inline feedback: snackbar **or** banner, not both    |
+----------+---------------------------------------------------+
```

**Scan now** is the primary toolbar action. **Edit standard** opens the canonical funding editor (dialog or inline panel). Search filters the **currently loaded rows** client-side or server-side; it must not trigger GitHub calls.

Row **Status** values (funding): Missing, Matches standard, Differs, Present but not compared, Not scanned (with short reason), Not visible to installation (funding blank). Trait badges: Archived, Fork, Private. Optional note chips: organisation default funding file seen; extra root `FUNDING.yml`.

Do **not** render a Community Standards section, checklist columns, or placeholder cards for future hygiene work.

## Canonical standard editor

- Shown from **Edit standard** when no standard exists: blocking `MudAlert` on the funding section with primary action **Set standard** (same editor).
- Editor: multiline field (YAML text), **Save** and **Cancel**; reject empty or whitespace-only input with UK English validation copy.
- When a standard exists: show last saved time; after edit, page warns that row labels reflect the previous standard until the next finished run (FR-019).

## Interaction notes

- **Scan now**: enqueue inventory; acknowledge within a few seconds via snackbar or inline success (`role="status"`). If a run is already queued or in progress, show informative copy that a run is already underway; do not start a duplicate walk.
- Page load queries SQL only (plus optional single catalogue list refresh when cache is cold per FR-002). No per-repository GitHub requests from Web.
- **Needs attention** chip / count: non-archived repositories that are missing or differ only.
- Repository name search combines with visible rows; changing filters does not clear search unless specified in implement.
- **GitHub** row action opens the repository in a new tab (`noopener`).
- Hosted sign-in: surface access kind (installation vs PAT). When installation credential is missing or revoked, banner explains that hosted overnight inventory cannot run; do not imply success.
- When the latest run **failed**, show last completed or partial snapshot in the table and a prominent banner for the failed attempt (not a clean bill of health).
- When results are **stale** (> 36 hours), still show data with stale banner and last finished time.
- Partial run: keep successful rows; mark unchecked repositories **Not scanned** with `FailureReason`; do not show them as missing.
- Catalogue entries added after the last run: **Not yet scanned** (not missing) until a run includes them.
- One user-facing outcome for scan acknowledgement: prefer snackbar **or** `MudAlert`, not both (constitution chrome).

## State variants

| State | Presentation |
|-------|----------------|
| **No inventory yet** | `MudAlert` (info): explain that no inventory has finished. No data grid presented as if every repository were healthy. Offer **Scan now** if credentials allow. |
| **Loading** | `MudSkeleton` on summary chips and table; toolbar remains usable where safe. |
| **Fresh completed run** | Default layout; run metadata shows Fresh; summary chips populated. |
| **Stale** | `MudAlert` (warning): results older than 36 hours; include last finished time. Table remains visible. |
| **Partial** | Run metadata shows Partial; banner notes incomplete walk; **Not scanned** rows with reason. |
| **Latest run failed** | `MudAlert` (error) for failed attempt; table shows previous completed/partial snapshot. |
| **No canonical standard** | Banner on funding section: set a standard before match/differ labels apply; **Present but not compared** allowed for checked repos when FR-003 applies. |
| **Standard drift** | Warning that visible statuses were compared with a previous standard until the next finished run. |
| **Hosted inventory unavailable** | Banner: hosted overnight inventory cannot run (installation credential missing or revoked). |
| **Run in progress** | Optional subtle status on run metadata or chip; scan now feedback uses "already underway" when coalesced. |
| **Empty open-source set** | No in-scope repositories: explain that none are tagged `open-source` in the linked catalogue; link mentally to Repositories (no reclassification UI here). |
| **Error loading snapshots** | `MudAlert` (error) with retry; no silent blank table. |

## Accessibility notes

- Focus order: skip link → page `h5` → purpose line → toolbar (**Scan now**, **Edit standard**, search) → run metadata → primary banner (if any) → summary chips → table header → table rows.
- Page `h5` is the main heading inside `#main-content`.
- Icon-only controls: `MudTooltip` and `aria-label` (for example "Scan now", "Open on GitHub").
- Banners: `role="alert"` for errors and hosted-unavailable; `role="status"` for stale or in-progress informational copy.
- Table: sortable headers expose name; status cells use text labels, not colour alone.
- Live region: scan acknowledgement uses `aria-live="polite"` when implemented as snackbar.

## Responsive behaviour

- **Desktop (≥ 1024 px)**: Summary chips wrap in one or two rows; full table columns.
- **Compact tablet (744 px)**: Toolbar wraps; search full width below actions; chips wrap; table may hide non-essential columns (for example file path) with path in row detail or tooltip.
- **Phone (390 px)**: Toolbar stacks; primary **Scan now** remains visible without horizontal page overflow; table becomes stacked row cards (repository name, status, traits, GitHub action) per [product UI language](product-ui-language-wireframe.md).
- Nav: **Open source** item visible in drawer; no second mobile-only route.

## Copy and language

- UK English for all labels, banners, empty states, and buttons (for example "organise", "favour", **Scan now**, **Edit standard**, **Matches standard**, **Not visible to installation**).
- Do not use an em dash in UI copy; use spaced hyphen where needed.
- Purpose line must state that this is hygiene from stored inventory, not a second repository catalogue.

## Out of scope (this wireframe)

- Applying funding files to GitHub, editing topics, repository groups UI, Community Standards checklist display.
- History browser for past inventory runs.
- Stub sections for future open-source checks.

## References

- [spec.md US1-US4](../../specs/001-oss-funding-hygiene/spec.md)
- [repositories-wireframe.md](repositories-wireframe.md) (catalogue classification stays there)
- [product-ui-language-wireframe.md](product-ui-language-wireframe.md)
- [ADR-0046](../../adr/adr-0046-overnight-inventory-functions-and-sql.md), [ADR-0047](../../adr/adr-0047-oss-funding-inventory-topology.md) (inventory topology)