---
layout: page
title: One-Click Migration
parent: User Guide
nav_order: 2
---

> ℹ️ **Phase 3 delivery** — The core One-Click Migration feature (labels and milestones) is complete. Project board configuration migration is planned for a later slice.

---

## Overview

One-Click Migration allows you to copy label taxonomies, milestones, and project board configurations from one GitHub repository to another in a single action. This is particularly useful when bootstrapping a new repository to match the conventions of an existing project.

Key goals of One-Click Migration:
- Eliminate the repetitive manual work of recreating labels and milestones in new repositories.
- Ensure consistency across related repositories.
- Provide a preview of changes before they are applied, so nothing is overwritten accidentally.

## How This Differs from Label Manager

One-Click Migration and Label Manager both help you keep repositories consistent, but they are aimed at different jobs.

- Use One-Click Migration when you want to make one or more target repositories resemble a known-good source repository.
- Use One-Click Migration when the goal is repository setup transfer rather than ongoing label maintenance.
- Use Label Manager when you want to manage labels as a living taxonomy across repositories over time.
- Use Label Manager when you need label-specific operations such as creating, renaming, recolouring, deleting, or re-synchronising labels without treating the whole repository setup as a migration.

For the current planned Phase 3 delivery slice, One-Click Migration covers labels and milestones. Project board configuration migration remains planned for a later slice.

## Example Use Cases

- You create a new repository and want it to inherit the labels and milestones from an existing repository you already run successfully. Use One-Click Migration.
- You already have several active repositories and want to rename a label, change its colour, or push one new label to all of them. Use Label Manager.
- You want to compare a source repository with one or more targets, review a preview, choose a conflict strategy, and then apply the result. Use One-Click Migration.
- You want an ongoing operational tool for label housekeeping and taxonomy enforcement. Use Label Manager.

---

## How to Use

### Step 1 — Select repositories

Choose one source repository and one or more target repositories. The source repository cannot also appear in the target list. The Preview and Apply buttons remain disabled until at least one source and one target are selected.

### Step 2 — Choose migration scope

Use the toggle switches to select which artefact types to include:

- **Labels** — copies the full label taxonomy from the source repository.
- **Milestones** — copies milestone titles, descriptions, states, and due dates.

Both are enabled by default. At least one must remain on or the Preview button will be unavailable.

### Step 3 — Choose a conflict resolution strategy

Select how existing artefacts in each target repository are handled when a match is found:

| Strategy | Behaviour |
|---|---|
| **Skip** | Conflicting items in the target are left unchanged. |
| **Overwrite** | Conflicting items are replaced with those from the source. A warning is shown before you confirm. |
| **Merge** | Conflicting items are replaced with source values; items that exist only in the target are preserved. |

### Step 4 — Preview changes

Click **Preview** to generate a read-only diff for each target repository. No changes are made at this stage.

The preview card for each target shows:

- **Labels** — tables listing labels to create, update, and delete, with colour, name, and description for each row.
- **Milestones** — tables listing milestones to create, update, and delete, with title, state, due date, and description for each row.

If the preview shows no actionable changes for a target repository, an information notice is displayed instead and the **Confirm and apply** button is not shown.

### Step 5 — Apply the migration

Once you are satisfied with the preview, click **Confirm and apply**. This button only appears when there is at least one actionable change across all target repositories.

If you selected the **Overwrite** strategy, an on-page warning is shown before destructive changes are applied.

Partial failures are reported per target repository — a failure for one target does not abort the remaining targets.

### Step 6 — Review the summary



After migration completes, a summary view is shown for each target repository.

- **Labels** and **Milestones** rows display the number of items created, updated, deleted, and skipped for each artefact type.
- Partial failures are reported per target repository, with error messages shown inline for any unsuccessful operations.
- If a target has no operations for an enabled artefact type, the summary still displays that artefact row with zero counts.

Project board configuration migration remains planned for a later slice and is not yet available.

---

## Configuration

One-Click Migration is configured entirely through the UI workflow. There are no `appsettings.json` entries specific to this feature.

- Conflict resolution strategy (skip, overwrite, or merge) is selected per migration run.
- Scope selection (labels and/or milestones) is toggled per migration run and defaults to both enabled.
- Deletion of artefacts present in the target but absent from the source is available via the **Overwrite** strategy.
