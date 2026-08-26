# Migration keep-`area/*` overwrite — issue checklist

Planning pass 2026-08-26. Automation created #471 and set parent/blocking links; follow-up labelling, issue body updates, and Project #8 sync were completed manually after rebase onto `main` (DEC-036 renumber).

## Created / updated

| Issue | Role | Applied in this run | Manual follow-up |
|-------|------|---------------------|------------------|
| [#464](https://github.com/markheydon/solo-dev-board/issues/464) | Enabler | On v1.1.0 with labels. Child #471 linked. Body updated to planned AC (DEC-036). | None. |
| [#471](https://github.com/markheydon/solo-dev-board/issues/471) | Test | Body created. Parent #464. Blocked by #464. Labels, milestone, and Project #8 applied. | None. |

## Planned #464 body (paste)

Use `/tmp` equivalent: the full planned body is the enabler description in the daily-planning run. Short form for paste:

DEC-036: Migration Overwrite **does** follow Label Manager keep-`area/*` (default on). Nested **Keep `area/*` labels** when Labels + Overwrite. Reuse `LabelTaxonomyPrefixes`. Wireframe: `plan/wireframes/one-click-migration-wireframe.md`. Parent: #88. Test: #471. Plan: `plan/migration-keep-area-labels-project-plan.md`.

## Relationships applied

**Sub-issues**

| Parent | Child | Relationship |
|--------|-------|--------------|
| #88 One-Click Migration (closed Feature) | #464 | Feature → Enabler |
| #464 | #471 | Enabler → Test |

**Blocking**

| Blocking | Blocked | Type |
|----------|---------|------|
| #464 | #471 | blocks |

## Project #8

#471 added with Status Todo and Priority Low. No Phase, Start Date, or Target Date (v1.1.0 leaf).

## Planned #464 body (full paste)

```markdown
## Description

Label Manager ([#446](https://github.com/markheydon/solo-dev-board/issues/446), [DEC-034](https://github.com/markheydon/solo-dev-board/blob/main/plan/DECISIONS.md#dec-034-label-manager-recommended-catalogue-omits-area-labels)) keeps `area/*` labels during Recommended taxonomy extra cleanup and Synchronise when **Keep `area/*` labels** is enabled. One-Click Migration still builds label overwrite previews without that rule: `MigrationService.BuildLabelPreview` treats every target-only label as a delete when the conflict strategy is Overwrite, and `KeptAreaLabels` is always empty on migration previews.

When the source repository omits `area/*` names (for example after the portable SoloDevBoard catalogue change), Overwrite can delete target `area/*` labels even though Label Manager would keep them by default.

**Product decision (DEC-036):** Migration label Overwrite **does** follow Label Manager keep-`area/*` behaviour. Default the nested control on. Do not leave the two features inconsistent.

## User Story

**As a** solo developer migrating labels with Overwrite,
**I want** target `area/*` labels kept by default,
**So that** One-Click Migration does not delete area labels that Label Manager would retain.

## Acceptance Criteria

- [ ] Given Labels is in scope and conflict strategy is Overwrite, when the migration form is shown, then a nested **Keep `area/*` labels** checkbox is visible and checked by default.
- [ ] Given Skip or Merge, when the form is shown, then the nested keep control is hidden or not actionable.
- [ ] Given Overwrite and **Keep `area/*` labels** checked, when preview and apply run, then target-only labels whose names start with `area/` are listed as kept and are not deleted.
- [ ] Given Overwrite and **Keep `area/*` labels** unchecked, when preview and apply run, then those `area/*` names are treated as ordinary target-only deletes.
- [ ] Skip and Merge label behaviour is unchanged. Milestone and Status column overwrite is unchanged.
- [ ] Preview and apply reuse `LabelTaxonomyPrefixes` (or equivalent shared helper) rather than duplicating the `area/` prefix rule.
- [ ] `website/content/docs/one-click-migration.md` describes the nested option and the default keep behaviour.
- [ ] Paired test issue coverage is in place.

## Related Epic / Milestone

v1.1.0. Catch-up on closed Feature [#88](https://github.com/markheydon/solo-dev-board/issues/88). No parent epic. Raised from review of PR [#462](https://github.com/markheydon/solo-dev-board/pull/462) implementing #446.

## Implementation References

- **Wireframe:** `plan/wireframes/one-click-migration-wireframe.md`
- **Parent issue:** [#88](https://github.com/markheydon/solo-dev-board/issues/88) (closed Feature; catch-up, no new wrapper)
- **Test issue:** [#471](https://github.com/markheydon/solo-dev-board/issues/471)
- **Related decisions:** DEC-036, DEC-034, DEC-010
- **Feature plan doc:** `plan/migration-keep-area-labels-project-plan.md`

## Implementation Notes

- Application: extend `IMigrationService.PreviewMigrationAsync` / `ApplyMigrationAsync` with `keepAreaLabels` defaulting to `true`. Filter Overwrite label deletes in `BuildLabelPreview` and the apply path using `LabelTaxonomyPrefixes.IsAreaLabel`. Populate `LabelSyncRepositoryPreviewDto.KeptAreaLabels` when keep is on.
- App: nested `MudCheckBox` on `/migrate` when Labels scope is on and strategy is Overwrite. Match Label Manager wording (**Keep `area/*` labels**). Prefer MudBlazor layout primitives and utility classes; no new page and no bespoke CSS unless a genuine gap appears.
- Do not add app-settings ignore lists. Do not change milestone or Status column overwrite.
- Layers: Application (preview/apply), App (control + preview caption), User Guide, tests (see test strategy).

## Out of scope

- Configurable keep-prefix lists in app settings.
- Changes to milestone or Status column migration.
- New Feature or Epic wrappers.

## Additional Context

Existing overwrite warning copy that Overwrite can remove target-only labels remains true for non-`area/*` names and for `area/*` when the nested box is unchecked.
```
