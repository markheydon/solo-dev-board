---
layout: page
title: Cross-Repo PM Workflow
parent: User Guide
nav_order: 7
---

> ⚠️ **Under Development** — This feature is planned for Phase 5 (v0.5.0). This page will be updated as the feature progresses.

---

## Overview

The Cross-Repo PM Workflow brings a structured, two-mode operating system into SoloDevBoard, replacing the manual AI prompts and scripts in [markheydon/github-workflows](https://github.com/markheydon/github-workflows) with a proper visual interface.

The system is built around two modes of operation:

- **PM Mode** (weekly or fortnightly) — Active curation: review your backlog across all repositories, resolve stalled work, and populate your project board with a realistic set of committed items for the next few days.
- **Work Mode** (daily) — Execution: the project board is the single pane of glass. Open it, pick the next item, and get things done. The optional Daily Focus view provides a quick morning nudge on what is most urgent today.

---

## Key Capabilities

### Daily Focus

A quick morning summary that answers "what should I work on right now?":

- Project board state: item count per status column and current active load
- Stalled items: anything in your Up Next column for three or more days
- Stalled PR reviews: pull requests in review for three or more days that need a merge, close, or return-to-progress decision
- Top three recommended work items, ranked by priority across all your repositories

### Backlog Review

A cross-repository prioritised view of all your open work:

- Urgent items (high-priority issues and PRs surfaced at the top)
- Items ready to start (unlabelled, unblocked stories and bugs across all repos)
- Blocked and deferred items (parked in Blocked or Ice Box)
- Neglected repositories (no issue or PR activity in the past 14 days, surfaced so nothing falls silently through the cracks)
- Items awaiting triage (no core label applied)

### Iteration Planning

A guided planning session that populates your project board's Up Next column:

- Shows current board capacity (Up Next + In Progress combined)
- Surfaces stalled Up Next items and asks you to resolve them before adding new work
- Lets you select issues and pull requests from across your repositories to commit to this week
- Optionally assigns a milestone to all planned items in a single step
- Configurable capacity limit to prevent over-committing

### Repo Management

Settings for cross-repo PM behaviour:

- Define a list of excluded repositories (personal, archived, or irrelevant repos that should not appear in PM operations)
- Repository health summary: issue and PR counts per repo at a glance

---

## How to Use

*Coming soon — this section will describe the step-by-step usage once the feature is built.*

---

## Configuration

*Coming soon — this section will describe capacity limits, excluded repos, and staleness thresholds.*
