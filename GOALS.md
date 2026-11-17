# Goals

**Project:** Solo Dev Board (code identifiers remain `SoloDevBoard`)  
**Owner:** Mark Heydon  
**Last updated:** 2026-11-17

---

## Why this exists

Solo developers maintaining multiple GitHub repositories waste time context-switching between tabs, boards, settings, and workflow runs. Solo Dev Board provides one Blazor Server application to triage issues, manage labels, audit health, migrate configuration, and plan work across repositories.

---

## Goals for v1.x

- **G1:** Ship a reliable hosted product on Azure Container Apps with GitHub App sign-in and operator-controlled admission.
- **G2:** Deliver practical multi-repo tooling (Label Manager, One-Click Migration, Planning, Actions Templates, Audit, Triage).
- **G3:** Keep end-user documentation and Playwright E2E coverage aligned for every published guide page.
- **G4:** Dogfood the app as the maintainer's daily solo-developer workflow across public repositories.
- **G5:** Preserve **lean local parity** alongside G1: a solo developer MUST be able to run the product locally via Aspire with only web compute plus GitHub credentials (PAT-only local trusted mode and a lean AppHost graph), without mandatory SQL, Functions, storage, or Azure subscription, while every in-scope user-facing capability remains reachable through graceful degradation (on-demand or in-process alternatives where infrastructure is absent). Full-experience AppHost resources (scheduled inventory, durable snapshots) stay optional for hosted sign-in and production-style deployment (constitution Principle VII; ADR-0046).

---

## Success looks like

- A solo developer can complete routine GitHub housekeeping without leaving Solo Dev Board for most tasks.
- Contributors can dogfood and develop on a lean Aspire graph without provisioning the full inventory stack, yet still exercise in-scope features at reduced capability where background work is optional.
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
| 2026-11-17 | Added G5 lean local parity; success criterion for lean Aspire graph | Constitution v1.1.0 and ADR-0046 follow-up |