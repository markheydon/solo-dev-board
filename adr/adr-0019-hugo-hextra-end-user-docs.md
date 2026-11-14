# ADR-0019: Hugo (Hextra) for end-user docs on GitHub Pages

**Date:** 2026-07-26
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-019 during the repository workflow alignment chore (2026-11-14).

## Decision

End-user documentation is authored as a Hugo site with the Hextra theme (now under `website/` per DEC-023; formerly `user-docs/`) and published to GitHub Pages via the official Hugo GitHub Actions workflow. Repository-centric developer and operator documentation remains in `docs/` and is not served by Pages. Local preview uses `scripts/invoke-hugo-site.sh` (bash) or `scripts/Invoke-HugoSite.ps1` (PowerShell) with Podman or Docker so contributors do not need a local Hugo or Go toolchain. Reject returning to Jekyll/`docs/` as the Pages source, and reject mixing operator deployment guides into the published end-user site.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
