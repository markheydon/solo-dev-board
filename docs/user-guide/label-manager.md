---
layout: page
title: Label Manager
parent: User Guide
nav_order: 3
---

## Overview

The Label Manager provides a unified interface for creating, editing, deleting, and synchronising GitHub labels across multiple repositories. Rather than managing labels repository by repository through the GitHub web interface, you can define a canonical label taxonomy once and push it to all relevant repositories.

Key goals of the Label Manager:

- Create labels across multiple repositories in one operation.
- Edit existing labels across selected repositories.
- Delete labels from selected repositories safely.
- Identify gaps where labels are missing.


## How to Use

The consolidated label view and bulk CRUD operations are available on the `Labels` page.

Current interactions:

1. Select one or more active repositories in the repository selector.
2. Select `Load selected repositories` to build a consolidated label view.
3. Use the `Filter` input to narrow results by label name.
4. Use `New label` to create labels for selected repositories.
5. Use row-level `Edit` and `Delete` actions to modify existing labels.

The create and edit dialogs include:

- `Label name` input.
- `MudColorPicker` for choosing a valid hexadecimal colour value.
- Optional description input.
- Repository selection controls scoped to valid repositories.

Planned interactions include:

- Validation and feedback improvements for partial API failures.
- Additional bulk operations for standard label templates.


## Applying Recommended Taxonomy

You can apply a recommended label taxonomy to selected repositories using a preview and confirm workflow.

1. Select one or more active repositories in the repository selector.
2. Choose a recommended strategy.
3. Select `Preview recommended taxonomy` to review proposed changes per repository.
4. Confirm or cancel before any changes are applied.
5. Review the per-repository summary after apply completes.

Current built-in strategies:

- `SoloDevBoard` strategy.
- `GitHub default` strategy.

The preview shows the labels that will be created, updated, and skipped for each selected repository. Labels that already match the selected strategy exactly are skipped, and no redundant API update call is made for those labels.

The apply summary is shown per repository and includes created, updated, and skipped counts. If one repository fails due to a GitHub API error, the summary marks that repository with an error while still showing successful outcomes for other repositories.

Strategies are currently built in for the MVP stage. Future releases can externalise strategy definitions so custom strategies can be managed without code changes.


## Synchronise Labels Workflow

The Label Manager allows you to synchronise labels from a source repository to one or more target repositories, ensuring consistent taxonomy across your projects.

### Selecting Source and Target Repositories
- Choose a source repository whose labels will be used as the reference.
- Select one or more target repositories to receive synchronised labels.

### Preview Before Apply
- After selecting repositories, initiate the synchronisation preview.
- The preview displays, for each target repository, which labels will be created, updated, deleted, or skipped.
- Skipped labels are those already matching the source exactly; no action is taken for these.

### Duplicate Submission Prevention
- While synchronisation is running, the apply button is disabled to prevent duplicate submissions.
- An in-progress indicator is shown until the operation completes.

### Summary and Partial Failure Reporting
- After synchronisation, a summary is shown for each target repository.
- Partial failures (such as API errors affecting only some labels) are reported per repository, with guidance for retry or manual intervention.

This workflow ensures you can preview all changes before applying, avoid redundant updates, and receive clear feedback on the outcome of each synchronisation operation.


## Configuration

*Coming soon — this section will describe configuration options for the Label Manager.*

Planned configuration options include:

- Default repository selection behaviour.
- Optional label naming conventions per repository group.
- Operational safeguards for high-impact bulk updates.
