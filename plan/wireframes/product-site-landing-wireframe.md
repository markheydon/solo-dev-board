# Product site landing wireframe

## Purpose

- Introduce SoloDevBoard to visitors who are not yet in the app.
- Route primary traffic to the User Guide and the GitHub repository.
- Surface shipped product features without duplicating guide prose.

## User goals

- Understand what SoloDevBoard is and who it is for (solo developers on GitHub.com).
- Open the User Guide or the source repository in one click.
- Scan shipped features and jump to the matching guide page.

## Global navigation

```
+------------------------------------------------------------------+
| SoloDevBoard    User Guide   About   [Search]   GitHub   [theme] |
+------------------------------------------------------------------+
```

- **User Guide** → `/docs/`
- **About** → `/about/`
- **GitHub** → repository (new tab)
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
| [ User Guide ]    [ View on GitHub ]                             |
|                                                                  |
| Open source. Self-host with Aspire or run locally — see repo docs.|
+------------------------------------------------------------------+
| Feature grid (3 columns, from guide front matter `landing: true`)|
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
- Primary CTA: User Guide (`/docs/`). Secondary: GitHub repo.
- Feature cards link to `/docs/<feature>/`; subtitles come from guide front matter `landingSubtitle`.
- Do not list in-app About or Appearance as landing pillars.
- Do not advertise paid tiers, Marketplace, or a public hosted URL.

### Accessibility

- CTAs are real links with visible text.
- Feature grid cards are keyboard-focusable links.
- Colour contrast follows Hextra theme defaults.

### Responsive behaviour

- Feature grid collapses to fewer columns on narrow viewports (Hextra `feature-grid`).
