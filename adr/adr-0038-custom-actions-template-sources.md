# ADR-0038: Custom Actions template sources

**Date:** 2026-09-02 (amended 2026-09-04)
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-038 during the repository workflow alignment chore (2026-11-14).

## Decision

Actions Templates may load **one additional GitHub repository** (`owner/name`) as a custom catalogue. The custom source UI uses a single-select **Repository Selector** (same active catalogue as apply targets) **plus** a manual `owner/name` field for repositories outside that list; both resolve to one source value and **Load templates** is explicit. The page lists YAML files at `.github/workflows/*.yml` and `*.yaml` (top-level only), infers `{{token}}` placeholders as required string parameters, and merges those templates into the same browser as built-ins with a source badge. Last-used source is stored in **browser localStorage** and pre-selects the catalogue repo when present, otherwise pre-fills the manual field; load is otherwise session-scoped. Built-in templates always remain visible when a custom file shares a workflow path. Public DTOs must use **stable string template identifiers** so custom paths do not collide with built-in integer ids. Private sources use the existing GitHub token. Server-backed persistence of sources waits on an Aspire product store ([#391](https://github.com/markheydon/solo-dev-board/issues/391)). Reject sidecar manifests, GitHub starter-workflow `properties.json`, appsettings-only registration, multiple concurrent custom sources, and in-app YAML authoring for this increment.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
