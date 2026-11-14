# Goals

**Project:** SoloDevBoard  
**Owner:** Mark Heydon  
**Last updated:** 2026-11-14

---

## Why this exists

Solo developers maintaining multiple GitHub repositories waste time context-switching between tabs, boards, settings, and workflow runs. SoloDevBoard provides one Blazor Server application to triage issues, manage labels, audit health, migrate configuration, and plan work across repositories.

---

## Goals for v1.x

- **G1:** Ship a reliable hosted product on Azure Container Apps with GitHub App sign-in and operator-controlled admission.
- **G2:** Deliver practical multi-repo tooling (Label Manager, One-Click Migration, Planning, Actions Templates, Audit, Triage).
- **G3:** Keep end-user documentation and Playwright E2E coverage aligned for every published guide page.
- **G4:** Dogfood the app as the maintainer's daily solo-developer workflow across public repositories.

---

## Success looks like

- A solo developer can complete routine GitHub housekeeping without leaving SoloDevBoard for most tasks.
- CI and CD stay fast, observable, and documented (`ci.yml`, `playwright.yml`, Aspire deploy).
- Architectural choices are traceable through `adr/` rather than scattered notes.

---

## Kill criteria

- Abandonment of GitHub as the sole provider (out of product scope unless explicitly replanned).
- Replacing Blazor Server with a SPA stack without a major ADR and scope revision.

---

## What this is not for

See [`SCOPE.md`](SCOPE.md) and [`plan/SCOPE.md`](plan/SCOPE.md) for out-of-scope items (team collaboration, non-GitHub providers, native mobile apps, marketplace monetisation in v1.0).

---

## Revision history

| Date | Change | Reason |
| --- | --- | --- |
| 2026-11-14 | Initial goals from legacy AGENTS.md / plan scope | Repo workflow alignment chore |