# SoloDevBoard visual language

Agents must choose MudBlazor components using this file together with [COMPONENT-CHOOSER.md](COMPONENT-CHOOSER.md). The app already has working features; the gap is **page chrome that feels considered**, not new widgets or custom CSS.

Official catalogue: https://mudblazor.com/components/overview. When MudBlazor is upgraded, follow **After a MudBlazor upgrade** at the end of this file (and the baseline note in `SKILL.md`).

---

## What “good” looks like on a SoloDevBoard page

Every authenticated feature page should read as the same product:

1. **Page header** — `MudText` `Typo.h5` (or `h4` only on hub pages) plus one sentence of `Typo.body2` `Color.Secondary` that states what the page is for.
2. **Command strip** — `MudToolBar` or a `MudStack Row="true"` of actions, not a scattered row of equally loud `Color.Primary` buttons.
3. **Primary surface** — one `MudPaper` `Elevation="1"` (or `MudCard` when the block is a discrete record). Do not wrap every field in its own paper.
4. **Loading** — `MudSkeleton` that matches the eventual layout for content-heavy regions; small `MudProgressCircular` only next to the control that is waiting (selectors, buttons).
5. **Empty** — `MudAlert` `Severity.Info` (or `Warning` when setup is required) with a single next action, not a blank paper.
6. **Error** — `MudAlert` `Severity.Error` at the top of the affected section with retry, per [DEC-035](../../../plan/DECISIONS.md#dec-035-transient-feedback-via-snackbar).
7. **Success** — snackbar, not a second banner that repeats the same text.

Do not introduce `MudFab`, `MudCarousel`, `MudChart`, or decorative `MudTimeline` on operational pages. Those exist in MudBlazor and are almost always the wrong choice here.

---

## Decision tree (use this before picking a control)

```
Need a full-page workflow with ordered steps?
  yes → MudStepper (Migration-style wizards only; do not invent a second app shell)
  no  ↓

Need two resizable panes (diagram + detail, catalogue + preview)?
  yes → MudSplitPanel
  no  ↓

Need a compact mode switch (Open source / All, list / grid)?
  yes → MudToggleGroup<T>
  no  ↓

Need overflow actions on a row or app bar?
  yes → MudMenu + MudIconButton (always MudTooltip on icon-only)
  no  ↓

Need to hide advanced fields?
  yes → MudCollapse or MudExpansionPanels
  no  ↓

Need a date span (from / to)?
  yes → MudDateRangePicker (not two unrelated MudDatePicker fields)
  no  → COMPONENT-CHOOSER.md
```

---

## Page chrome recipe

```razor
<MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="pa-4">
    <MudStack Spacing="3">
        <MudStack Spacing="1">
            <MudText Typo="Typo.h5">Page title</MudText>
            <MudText Typo="Typo.body2" Color="Color.Secondary">One-line purpose.</MudText>
        </MudStack>

        <MudToolBar Gutters="false" Dense="true" Class="pl-0">
            <MudButton Variant="Variant.Filled" Color="Color.Primary">Commit</MudButton>
            <MudButton Variant="Variant.Outlined" Color="Color.Secondary">Reload</MudButton>
            <MudSpacer />
            <MudTextField @bind-Value="_filter" Label="Filter" Variant="Variant.Outlined"
                          Adornment="Adornment.End" AdornmentIcon="@Icons.Material.Filled.Search"
                          Immediate="true" Class="mud-width-full" Style="max-width: 280px;" />
        </MudToolBar>

        <MudPaper Class="pa-4" Elevation="1">
            @* main content *@
        </MudPaper>
    </MudStack>
</MudContainer>
```

`MainLayout` already supplies `MudLayout`, `MudAppBar`, `MudDrawer`, and `MudMainContent`. Do not nest a second layout shell.

### Narrow viewports (web, not a native app)

SoloDevBoard is used in mobile Safari and Chrome as well as on desktop. There is no native iOS or Android client. Prefer the same chrome recipe at every width:

- **Phone (~390 CSS px):** app bar chips and command labels must not overlap or overflow the viewport. Wrap the toolbar. Put the filter full width under actions. Overflow extra commands into `MudMenu`.
- **Compact tablet (~744 CSS px, iPad mini portrait):** wrapping toolbars and stacked selects are enough; do not invent a card-only layout unless a grid actually overflows.
- **Do not** add `MudFab` navigation, a second bottom nav, or a PWA-only shell.

Parked delivery for a systematic pass is [#411](https://github.com/markheydon/solo-dev-board/issues/411) under [#527](https://github.com/markheydon/solo-dev-board/issues/527).

---

## Loading, empty, and error (do not mix these up)

| State | Component | Do not. |
|-------|-----------|---------|
| First paint of a table, card grid, or KPI row | `MudSkeleton` with `Animation.Wave`, heights close to the real rows | A lone centred spinner in an otherwise empty paper. |
| Waiting on one select or button | `MudProgressCircular` `Size.Small` beside that control | Blocking `MudOverlay` for a repository list fetch. |
| No data after a successful load | `MudAlert` `Severity.Info` plus the action that creates or changes the filter | Fake table headers with zero rows and no explanation. |
| Configuration missing (no board, no PAT scope) | `MudAlert` `Severity.Warning` with a link or button into the setup page | A `MudDialog` that the user did not open. |
| Load failed | `MudAlert` `Severity.Error` with retry | Snackbar-only errors that vanish while the page stays empty. |

Audit Dashboard already uses skeletons well; copy that pattern rather than the spinner-in-paper pattern on catalogue pages.

---

## Density and elevation

- Default paper elevation is **1**. Selected cards may use **4**. Outlined `Elevation="0"` is for secondary callouts only.
- Prefer `Dense="true"` on toolbars, tables, and chips in data-heavy views.
- Prefer `Variant.Outlined` on fields.
- Do not mix `pa-2`, `pa-3`, `pa-4`, and `pa-6` on sibling papers; use **`pa-4`** for section papers and **`pa-2`** only inside nested lists.

---

## Colour and icons

- Semantic colour comes from `Color.*` / `Severity.*` and `SoloDevBoardTheme`. Do not hard-code hex except GitHub label colours (data, not brand).
- `Color.Primary` is for the one commit action in a region. `Color.Secondary` is reload / preview. `Color.Error` is destructive. `Variant.Text` is cancel.
- Icons are `Icons.Material.Filled` unless the control is the theme toggle (existing Rounded/Outlined exception). Pair every `MudIconButton` with `MudTooltip` and an `aria-label`.

---

## Components that exist in MudBlazor but are usually wrong here

| Component | When it is acceptable. | Default. |
|-----------|------------------------|----------|
| `MudFab` / `MudFabMenu` | None on current pages. | Do not add a floating action button. |
| `MudChart` | None. | Do not chart GitHub counts in-app. |
| `MudCarousel` | None. | Do not rotate operational content. |
| `MudStepper` | A single linear apply/preview wizard. | Prefer stacked papers if there are only two steps. |
| `MudHotKey` | Documented keyboard shortcuts (Triage). | Do not invent undocumented global hotkeys. |
| `MudExitPrompt` | Unsaved triage or migration preview. | Use only when leaving would drop in-progress work. |
| `MudBreakpointProvider` | Tests or rare layout that must read breakpoints in C#. | Prefer `MudHidden` and `MudGrid` in markup. |

`MudChat` is not used in SoloDevBoard. Do not add it unless current MudBlazor docs show it and a wireframe explicitly calls for a chat transcript.

---

## After a MudBlazor upgrade

Do this in the **same** pull request as the package bump (Dependabot or manual). Do not leave it for later.

1. Compare https://mudblazor.com/components/overview with this file and `COMPONENT-CHOOSER.md`.
2. Update the baseline version in `../SKILL.md` only (not in every reference file).
3. Add newly relevant components to the decision tree; do not copy the entire catalogue into every page.
4. If the catalogue did not change in a way that affects SoloDevBoard, say so in the PR additional notes.