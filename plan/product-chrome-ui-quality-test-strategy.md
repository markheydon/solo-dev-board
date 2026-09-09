# Product chrome and UI quality — test strategy

## Testing scope

- **Visual language:** header, toolbar hierarchy, skeleton vs spinner, empty/error alerts on representative pages (Repositories, Labels, Audit, Triage, Planning Daily Focus).
- **Branding:** deferred until #397 is un-parked; then favicon, apple-touch, app-bar mark, static error pages.

## Quality objectives

- Existing Playwright journeys still pass with unchanged `data-testid` values where possible.
- No duplicate snackbar plus inline alert for the same outcome (DEC-035).
- Icon-only controls expose accessible names.

## Approach

- **bUnit:** page header and toolbar presence; skeleton vs spinner branches on one catalogue page and Audit.
- **Playwright:** loaded-state shells already in `tests/E2E`; extend assertions for heading + primary button colour hierarchy only where the guide describes them.
- **Docs capture:** recapture screenshots if chrome changes materially (`DocsCapture:Enabled=true`, public catalogues only).

## Risks

- Locator churn if papers are restructured. Prefer wrapping existing test ids rather than renaming.

## ISO 25010 emphasis

Usability and maintainability (agents keep using the skill) over new functional suitability.
