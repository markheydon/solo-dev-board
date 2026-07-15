---
layout: page
title: Board Rules Visualiser
parent: User Guide
nav_order: 4
---

> ⚠️ **Partial delivery** — Repository and supported board selection is available, and the board columns plus supported transitions are now visualised. Full rule inspection, warnings, and compare mode are delivered in later slices.

---

## Overview

The Board Rules Visualiser displays the board states and supported transitions for a GitHub Project v2 board. It helps you understand how issues and pull requests move between columns without reading raw configuration payloads.

For SoloDevBoard itself, the canonical project board now includes an **Up Next** planning state for the next short-horizon batch of stories, enablers, and tests, plus a **Focus Order** field that sequences that batch on the Story Board.

Key goals of the Board Rules Visualiser:
- Make project board states and supported transitions visible and understandable at a glance.
- Help diagnose board flow issues by surfacing the supported column progression for a selected board.
- Provide a foundation for the One-Click Migration feature (migrating board rules between repositories).

---

## How to Use

### Select a repository and project board

1. Open **Board Rules Visualiser** from the navigation menu or dashboard.
2. Use the repository search box to choose one active repository. SoloDevBoard reuses the same repository selector pattern as Label Manager and Migration.
3. After a repository is selected, SoloDevBoard loads GitHub Project v2 boards linked to that repository.
4. Choose a supported project board from the **Project board** dropdown. Supported boards must expose a **Status** field.
5. When a board is selected, the visualisation area displays board states and the supported transitions between adjacent columns.

### What you can do now

- View board states derived from the selected Project v2 board's Status field.
- Inspect the supported adjacent transitions between board columns.
- See a warning when the board metadata is only partially visible.
- Click rule nodes to inspect the full rule name, trigger and action configuration.
- See potential rule conflicts highlighted when duplicate triggers or incomplete rule details are present.
- Reload repositories or project boards if GitHub API requests fail.

### What is coming later

- Full rule inspection for board automation rules and trigger conditions.
- Warning details for conflicting or unsupported rule patterns.
- Compare mode for differences between repositories.

### Empty and unsupported states

- **No repository selected:** The visualisation area prompts you to choose a repository and project board.
- **No supported boards:** If the repository has no GitHub Project v2 board with a Status field, SoloDevBoard explains why the visualiser cannot continue and does not show the diagram state.
- **Loading:** Progress indicators appear while repositories or project boards are loading.
- **Errors:** If GitHub cannot be reached, an error message appears with a retry action.

---

## Configuration

*Coming soon — this section will describe configuration options for the Board Rules Visualiser.*

Planned configuration options include:
- Choosing between GitHub classic projects and GitHub Projects (v2).
- Layout options for the visualisation diagram.
