---
layout: page
title: One-Click Migration
parent: User Guide
nav_order: 2
---

> ⚠️ **Under Development** — This feature is planned for Phase 3. This page will be updated as the feature progresses.

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

*Coming soon — this section will describe the step-by-step migration process once the feature is complete.*

Planned interactions include:
- Selecting a source repository and a target repository.
- Choosing which artefacts to migrate (labels, milestones, board columns).
- Previewing a diff of changes (what will be added, updated, or removed).
- Confirming and applying the migration.
- Reviewing a post-migration summary.

---

## Configuration

*Coming soon — this section will describe any configuration options for One-Click Migration.*

Planned configuration options include:
- Conflict resolution strategy (skip, overwrite, or merge conflicting items).
- Whether to delete artefacts in the target that do not exist in the source.
