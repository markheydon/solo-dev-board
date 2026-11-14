# ADR-0033: Actions Templates and Planning product names

**Date:** 2026-08-23
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-033 during the repository workflow alignment chore (2026-11-14).

## Decision

User-facing names are **Actions Templates** (GitHub Actions workflow template library at `/actions-templates`) and **Planning** (cross-repo planning hub at `/planning`, Iteration tab at `/planning/iteration`). Routes, C# namespaces (`Features.ActionsTemplates`, `Planning`), and `data-testid` prefixes follow the product names. Legacy URL aliases (`/workflows`, `/pm-workflow`, `/audit`) are not supported. Domain types `WorkflowFile` and `WorkflowRun` remain for GitHub workflow file/run data (Audit KPI and template apply). This repository's GitHub `area/*` labels use `area/actions-templates` and `area/planning`; `area/dashboard` is Audit Dashboard only. Label Manager's built-in recommended catalogue omits `area/*` ([#446](https://github.com/markheydon/solo-dev-board/issues/446)). Reject reverting nav labels to **Workflows** / **PM Workflow** or mapping Planning work to `area/dashboard`.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
