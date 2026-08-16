# Product site landing wireframe

## Purpose

- Introduce SoloDevBoard as a product and a project: what the app is, why it exists, and how the repository is run.
- Keep the home page short: summary, one path into About, then a capability grid.
- Keep the User Guide and GitHub as header (and GitHub icon) destinations, not competing hero buttons.

## User goals

- Understand what SoloDevBoard is and who it is for (solo developers on GitHub.com).
- Open About for the full origin and how-the-project-is-run story.
- Scan shipped capabilities as icon + name + one-liner tiles, matching the in-app nav icons.
- Reach the User Guide or GitHub from the site chrome when they want more.

## Global navigation

```
+------------------------------------------------------------------+
| SoloDevBoard    About   User Guide   [Search]   GitHub   [theme] |
+------------------------------------------------------------------+
```

- **About** → `/about/` (first item after the title).
- **User Guide** → `/docs/`.
- **GitHub** → repository (header icon; no duplicate hero button).
- Theme toggle and search unchanged from Hextra defaults.

## Landing page (`/`)

```
+------------------------------------------------------------------+
| [Released v1.0.0]                                                |
|                                                                  |
| SoloDevBoard                                                     |
| A single pane of glass for solo developers managing GitHub         |
| workloads across multiple repositories.                          |
|                                                                  |
| Three short paragraphs: problem + what the app is; github-workflows |
| PM/Work lineage; AI-collaborator experiment under human direction.|
|                                                                  |
| [ Learn more about the project ]  → /about/                      |
+------------------------------------------------------------------+
| Feature grid (3 columns; 7 tiles; icon + name + one-liner)       |
| Icons match the in-app nav (Material Symbols).                   |
| Tiles are not links.                                             |
+------------------------------------------------------------------+
| Footer: copyright, MIT licence and repo docs/, release, Hugo     |
+------------------------------------------------------------------+
```

### Interaction notes

- Layout: `hextra-home`.
- Hero badge shows release tag from `params.releaseVersion` (injected on tag deploy).
- Single on-page CTA: Learn more about the project (`/about/`).
- Feature tiles are **not** links. Titles, one-liners, and icons come from guide front matter (`landing: true`, `landingSubtitle`, `landingIcon`) so claims stay aligned with published guides (DEC-023).
- Do not list in-app About or Appearance as landing pillars.
- Do not advertise paid tiers, Marketplace, or a public hosted URL.
- Do not invent filler cards to force a 3×3 or 4×4 cell count.

### Accessibility

- CTA is a real link with visible text.
- Feature icons are `aria-hidden`; the heading is the accessible name.
- Colour contrast follows Hextra theme defaults.

### Responsive behaviour

- Feature grid: one column, then two, then three (`hx:sm:grid-cols-2 hx:lg:grid-cols-3`).
