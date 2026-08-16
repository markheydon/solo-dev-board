# Product site landing wireframe

## Purpose

- Introduce SoloDevBoard as a **product and a project**: what the app is, why it exists, and how the repository is run.
- Route primary traffic to GitHub and the About section.
- Keep the User Guide as a how-to path, not the story of the home page.

## User goals

- Understand what SoloDevBoard is and who it is for (solo developers on GitHub.com).
- Open the GitHub repository or the About section in one click.
- Read why the project exists and how it is run without first entering the User Guide.
- Scan shipped capabilities as a secondary product summary.

## Global navigation

```
+------------------------------------------------------------------+
| SoloDevBoard    About   User Guide   [Search]   GitHub   [theme] |
+------------------------------------------------------------------+
```

- **About** → `/about/` (first item after the title; this is the project story).
- **User Guide** → `/docs/` (how to use the app).
- **GitHub** → repository (new tab).
- Theme toggle and search unchanged from Hextra defaults.

## Landing page (`/`)

```
+------------------------------------------------------------------+
| [Released v1.0.0]  (badge — git tag on tag builds; unreleased in CI) |
|                                                                  |
| SoloDevBoard                                                     |
| A single pane of glass for solo developers managing GitHub         |
| workloads across multiple repositories.                          |
|                                                                  |
| Pitch: GitHub spreads the work across tabs; this is the app you  |
| run locally or self-host.                                        |
|                                                                  |
| [ View on GitHub ]    [ About the project ]                      |
|                                                                  |
| MIT licence. Operator setup in repo docs. User Guide as text link.|
+------------------------------------------------------------------+
| The project (cards link into /about/)                            |
| +---------------------------+ +---------------------------+      |
| | Origin                    | | How the project is run    |      |
| | why it exists...          | | AI-collaborator experiment|      |
| +---------------------------+ +---------------------------+      |
+------------------------------------------------------------------+
| What the app does (capability tiles, not documentation links)    |
| Feature grid from guide front matter `landing: true`             |
| +----------------+ +----------------+ +----------------+           |
| | Audit Dashboard| | Label Manager  | | Repositories   |           |
| | subtitle...    | | subtitle...    | | subtitle...    |           |
| +----------------+ +----------------+ +----------------+           |
| ... (seven product features; omit draft pm-workflow)             |
+------------------------------------------------------------------+
| Footer: copyright, powered by Hugo/Hextra, release version       |
+------------------------------------------------------------------+
```

### Interaction notes

- Layout: `hextra-home` (existing Hextra landing layout).
- Hero badge shows release tag from `params.releaseVersion` (injected on tag deploy).
- Primary CTAs: GitHub repository and About (`/about/`). The User Guide is a text link in the hero footnote and remains in the nav; it is not a hero button.
- **Project cards** link to `/about/origin/` and `/about/how-we-work/`. Subtitles come from About page front matter (`landing: true`, `landingSubtitle`).
- **Feature tiles** are **not** links. They summarise shipped capabilities from guide front matter (`landing: true`, `landingSubtitle`) so claims stay aligned with published guides (DEC-023). How-to copy stays on `/docs/`.
- Do not list in-app About or Appearance as landing pillars.
- Do not advertise paid tiers, Marketplace, or a public hosted URL.

### Accessibility

- CTAs are real links with visible text.
- Project cards are keyboard-focusable links into About.
- Feature tiles are static content (headings and paragraphs), not fake buttons.
- Colour contrast follows Hextra theme defaults.

### Responsive behaviour

- Project cards: two columns, collapsing to one on narrow viewports.
- Feature grid collapses to fewer columns on narrow viewports (Hextra `feature-grid`).
