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


## Configuration

*Coming soon — this section will describe configuration options for the Label Manager.*

Planned configuration options include:

- Default repository selection behaviour.
- Optional label naming conventions per repository group.
- Operational safeguards for high-impact bulk updates.
