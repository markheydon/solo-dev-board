---
weight: 50
title: Board Rules Visualiser
landing: true
landingIcon: rule
landingSubtitle: "Visualise supported board states and transitions for GitHub Project v2 boards."
guideStatus: Partially Available
---

> **Scope note** — Repository and supported board selection, column and transition visualisation, rule inspection warnings, and compare mode are available now. Full automation-rule retrieval from GitHub remains a later slice.

---

## Overview

The Board Rules Visualiser displays the board states and supported transitions for a GitHub Project v2 board. It helps you understand how issues and pull requests move between columns without reading raw configuration payloads.

![Board Rules Visualiser showing board states and transitions for markheydon/solo-dev-board](/images/board-rules-visualiser/overview.png)

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
- Enable **Compare mode** to select a second repository and project board, then review column, rule, and visibility differences side by side.
- Reload repositories or project boards if GitHub API requests fail.

### Compare two repositories

1. Turn on **Compare mode** in the repository selector region.
2. Select the primary repository and project board as usual.
3. Choose a comparison repository and supported project board in the comparison selector region.
4. Review the side-by-side summaries and the differences panel when both boards are loaded.
5. Turn off compare mode to return to single-board inspection without losing your primary selection.

### What is coming later

- Full rule inspection for board automation rules and trigger conditions directly from GitHub.
- Deeper warning details for conflicting or unsupported rule patterns beyond the current heuristic checks.

### Empty and unsupported states

- **No repository selected:** The visualisation area prompts you to choose a repository and project board.
- **No supported boards:** If the repository has no GitHub Project v2 board with a Status field, SoloDevBoard explains why the visualiser cannot continue and does not show the diagram state.
- **Some linked boards inaccessible:** If GitHub reports more linked project boards than SoloDevBoard can load, a warning explains how many could not be read. This commonly happens for **private user-owned** Projects v2 boards under hosted GitHub App sign-in: GitHub may list the board as linked while the App token cannot read it. Public linked boards still load. Use PAT mode with the `read:project` scope, or make the project public.
- **Loading:** Progress indicators appear while repositories or project boards are loading.
- **Errors:** If GitHub cannot be reached, an error message appears with a retry action.
