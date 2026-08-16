# Public product site — project plan

## Feature summary

Turn the Hugo/Hextra site into the **public product site** for SoloDevBoard: marketing landing at `/`, User Guide at `/docs/`, narrative pages at `/about/`, canonical domain `https://solodevboard.com/`, and source tree renamed from `user-docs/` to `website/`. Stay on GitHub Pages with tag-only publish (DEC-021).

## Success criteria

- Landing page explains what SoloDevBoard is, who it is for, and links to User Guide and GitHub.
- User Guide content at `/docs/` remains accurate; per-feature guides are not rewritten as marketing copy.
- About section covers origin and AI-collaborator experiment honestly.
- Hugo builds in CI; tag deploy is ready for `solodevboard.com` with operator DNS steps documented.
- Release version stamped on tag builds; no "Early access" badge unless still true.
- Feature tiles on the landing derive from published guide front matter (no duplicated feature lists) and are capability summaries, not links into the User Guide.

## Key milestones

1. Planning artefacts and wireframes in `plan/`.
2. GitHub Feature issue + child stories/enabler/test; Project #8 sync.
3. `git mv user-docs website` and retarget CI/scripts/docs references.
4. Landing, About, nav, shared feature metadata, version stamp.
5. Hugo build verification and PR.

## Risks

| Risk | Mitigation |
|------|------------|
| `CNAME` committed before DNS ready | Document operator must complete DNS before first `v*` tag after merge. |
| Broken links after folder rename | Grep for `user-docs` and update all references in same PR. |
| Landing claims exceed shipped features | Generate cards from guide front matter; omit draft `pm-workflow.md`. |

## Work item hierarchy

- **Feature:** Public product site on `solodevboard.com`
  - **Story:** Hextra landing, nav IA, shared feature cards, version stamp
  - **Story:** Narrative About/origin/AI-experiment pages
  - **Enabler:** Rename to `website/`, canonical domain, CNAME, deploy `baseURL`
  - **Test:** Hugo published-route smoke + alignment doc update

## Manual linking required

| Child | Parent | Relationship |
|-------|--------|--------------|
| Landing story | #359 | #358 | sub-issue |
| About story | #360 | #358 | sub-issue |
| Domain/rename enabler | #361 | #358 | sub-issue (blocks deploy) |
| Hugo test issue | #362 | #358 | sub-issue |

GitHub CLI cannot set sub-issue relationships; link manually in the GitHub UI after creation.

## Implementation references

- Wireframes: `plan/wireframes/product-site-landing-wireframe.md`, `plan/wireframes/product-site-about-wireframe.md`
- Decisions: DEC-019, DEC-021, DEC-023 (new)
- Alignment: `tests/E2E/USER_DOCS_ALIGNMENT.md`
