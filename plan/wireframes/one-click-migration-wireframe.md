# One-Click Migration Page Wireframe

## Purpose
- Provide a workflow-first layout for the existing labels-and-milestones migration slice.
- Make source selection, target selection, migration scope, preview review, and results easier to scan before and after apply.
- Guide implementation for story #139 and the paired bUnit coverage in issue #140.

## User Goals
- Select one source repository and one or more target repositories without losing context.
- Choose the migration scope and conflict strategy in a clear control area.
- Review a preview that explains what will be created, updated, skipped, or deleted before applying the migration.
- See post-migration outcomes, including partial failures, in a deliberate summary region.

## Layout
```
+-----------------------------------------------------------------------+
| Page Header: One-Click Migration                                      |
| Intro copy: labels and milestones only for the current delivery slice |
+-----------------------------------------------------------------------+
| Workflow Controls                                                     |
| [Source Repository Select] [Target Repository Multi-Select]           |
| [Scope: Labels] [Scope: Milestones] [Conflict Strategy Select]        |
| [Preview Changes]                                    [Apply Migration]|
+-----------------------------------------------------------------------+
| Preview Region                                                        |
| +-------------------------------------------------------------------+ |
| | Target Repository A                                                | |
| |  - Labels: create 2, update 1, skip 3                             | |
| |  - Milestones: create 1, update 0, skip 2                         | |
| |  - Conflicts / warnings table                                     | |
| +-------------------------------------------------------------------+ |
| | Target Repository B                                                | |
| |  - Summary rows repeated per target                               | |
| +-------------------------------------------------------------------+ |
+-----------------------------------------------------------------------+
| Post-Migration Summary                                                |
| [Success / partial failure / error alert]                            |
| [Created | Updated | Deleted | Skipped counts by target]             |
+-----------------------------------------------------------------------+
| Feedback Region                                                       |
| [Status messages, warnings, validation, and next-step guidance]       |
+-----------------------------------------------------------------------+
```

## Interaction Notes
- Source and target selectors establish the workflow context and remain visible above preview and summary regions.
- Preview stays hidden until the source, at least one target, and a valid scope are selected.
- Apply remains disabled until a preview has been generated and there is actionable work to perform.
- Conflict strategy is explained inline because it materially changes preview and apply outcomes.
- Preview and post-migration summary are grouped by target repository so multi-target runs are easier to interpret.
- MudBlazor layout primitives and utility classes should be preferred over bespoke CSS.

## State Variants
- Empty state: Explain that a source repository, one or more target repositories, and at least one migration scope are required before preview can run.
- Loading state: Keep the workflow controls visible while preview or apply work is in progress, with the active region showing a busy state.
- Warning state: Surface conflicts, validation issues, or partial failures in a clear feedback region without hiding previously generated preview data.
- Success state: Show a concise result summary with created, updated, deleted, and skipped counts for each target repository.

## Accessibility Notes
- Focus order should move from workflow controls to preview, then to the post-migration summary and feedback region.
- All selectors, buttons, and alerts should expose accessible labels and descriptive helper text where needed.
- Status and validation messaging should use an `aria-live` polite region so workflow feedback is announced without being disruptive.
- Preview tables and summary counts should remain understandable when read linearly by assistive technologies.

## Responsive Behaviour
- Desktop: Keep workflow controls in a compact multi-column command surface above full-width preview and summary regions.
- Tablet: Allow the workflow controls to wrap into two rows while preserving the action buttons close to the selection controls.
- Mobile: Stack all controls vertically, keep preview cards grouped per target repository, and move secondary explanatory text below the primary actions.
