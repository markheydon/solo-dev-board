# ADR-0023: Public product site IA and canonical domain

**Date:** 2026-08-16
**Status:** Active

## Context

This record was migrated from legacy decision log entry DEC-023 during the repository workflow alignment chore (2026-11-14).

## Decision

The Hugo/Hextra site lives under `website/` (renamed from `user-docs/`) and is the single public product site: marketing landing at `/`, narrative About pages at `/about/`, and User Guide at `/docs/`. Canonical URL is `https://solodevboard.com/` (apex) on GitHub Pages with tag-only publish (DEC-021). The landing is a short product-and-project page: an About-derived summary, one Learn more about the project CTA, and capability tiles (icon + name + one-liner from published guide front matter, matching in-app nav icons) that each link as a whole card to the matching User Guide page. About is first in the site nav; GitHub is header chrome only. The User Guide is a how-to path, not the home-page narrative. Developer and operator documentation stays in `docs/` and is not served on the product domain (DEC-019). Reject a second Hugo site, a second publish pipeline, converting guide articles into marketing copy, or moving operator docs onto the product domain.

## Notes

See git history of `plan/DECISIONS.md` for full rationale, related links, and clarifications prior to migration.
