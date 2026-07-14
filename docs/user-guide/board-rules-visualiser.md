---
layout: page
title: Board Rules Visualiser
parent: User Guide
nav_order: 4
---

> ⚠️ **Under Development** — Repository and project board selection is available. The interactive diagram and rule inspection views are delivered in later slices.

---

## Overview

The Board Rules Visualiser displays the automation rules configured on your GitHub project boards as an interactive diagram. It makes it easy to understand how issues and pull requests flow between columns, which labels trigger transitions, and where bottlenecks may occur.

For SoloDevBoard itself, the canonical project board now includes an **Up Next** planning state for the next short-horizon batch of stories, enablers, and tests, plus a **Focus Order** field that sequences that batch on the Story Board.

Key goals of the Board Rules Visualiser:
- Make project board automation rules visible and understandable at a glance.
- Help diagnose automation issues where items are not flowing as expected.
- Provide a foundation for the One-Click Migration feature (migrating board rules between repositories).

---

## How to Use

### Select a repository and project board

1. Open **Board Rules Visualiser** from the navigation menu or dashboard.
2. Use the repository search box to choose one active repository. SoloDevBoard reuses the same repository selector pattern as Label Manager and Migration.
3. After a repository is selected, SoloDevBoard loads GitHub Project v2 boards linked to that repository.
4. Choose a supported project board from the **Project board** dropdown. Supported boards must expose a **Status** field.
5. When a board is selected, the visualisation area confirms the board context is ready. The interactive diagram arrives in a later delivery slice.

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
