---
layout: page
title: Label Manager
parent: User Guide
nav_order: 3
---

> ⚠️ **Under Development** — This feature is planned for Phase 2. This page will be updated as the feature progresses.

---

## Overview

The Label Manager provides a unified interface for creating, editing, deleting, and synchronising GitHub labels across multiple repositories. Rather than managing labels repository by repository through the GitHub web interface, you can define a canonical label taxonomy once and push it to all relevant repositories.

Key goals of the Label Manager:
- Maintain a single source of truth for your label taxonomy.
- Identify and resolve label inconsistencies across repositories (e.g. different colours for the same label name).
- Support bulk operations: create, rename, recolour, and delete labels across many repositories at once.

---

## How to Use

The read-only consolidated label view is now available on the `Labels` page.

Current interactions:
- Search and select one or more active repositories in the repository selector.
- Archived repositories are hidden by default in the selector.
- Use the label name filter input to narrow the visible rows.
- Review each row for label name, colour, description, repositories containing that label, and repositories missing that label.

Planned interactions include:
- Creating a new label and pushing it to multiple repositories simultaneously.
- Editing a label's name, colour, or description.
- Synchronising a target repository's labels to match a source repository or a defined template.
- Flagging and resolving label conflicts.

---

## Configuration

*Coming soon — this section will describe configuration options for the Label Manager.*

Planned configuration options include:
- Default label template (the label taxonomy from `plan/LABEL_STRATEGY.md` will be available as a built-in template).
- Repositories to include in the label sync scope.
