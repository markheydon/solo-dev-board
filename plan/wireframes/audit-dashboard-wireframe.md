# Audit Dashboard Page Wireframe

## Purpose
- Provide a centralised view of repository health, KPIs, and audit results.
- Enable users to filter repositories and review key metrics at a glance.

## User Goals
- Filter repositories using command surface controls.
- View KPI summary cards for quick health assessment.
- Drill down into health indicators and audit details.

## Layout
```
+-------------------------------------------------------------+
| Command Surface: [Repository Filter] [Date Range]           |
+-------------------------------------------------------------+
| KPI Summary Cards:                                          |
| +----------------+  +----------------+  +----------------+ |
| | Open Issues    |  | PRs Awaiting   |  | Security Alerts| |
| | 12             |  | Review: 3      |  | 0              | |
| +----------------+  +----------------+  +----------------+ |
+-------------------------------------------------------------+
| Health Indicators Section:                                  |
| +----------------+  +----------------+  +----------------+ |
| | Repo-1: Healthy|  | Repo-2: At Risk|  | Repo-3: Error  | |
| +----------------+  +----------------+  +----------------+ |
+-------------------------------------------------------------+
| Feedback Region: [Status, errors, confirmations]            |
+-------------------------------------------------------------+
```

## Interaction Notes
- Command surface filters update KPI cards and health indicators in real time.
- KPI cards are clickable for drill-down details.
- Health indicators expand for more information as data volume grows.
- Use tabs for high-level navigation; switch to expansion panels if indicator count increases significantly.

## State Variants
- Empty state: Show onboarding prompt if no repositories are selected.
- Loading state: Display spinner overlay on KPI cards and indicators.
- Error state: Show error in feedback region.

## Accessibility Notes
- Focus order: Command surface → KPI cards → health indicators → feedback region.
- ARIA: KPI cards use `aria-label`, health indicators use `aria-describedby`.
- Live region: Feedback region uses `aria-live="polite"`.

## Responsive Behaviour
- Desktop: KPI cards and health indicators display in grid layout.
- Mobile: Cards and indicators stack vertically, command surface collapses into dropdown.
