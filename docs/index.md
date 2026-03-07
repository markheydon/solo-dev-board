---
layout: default
title: SoloDevBoard
nav_order: 1
---

> **A single pane of glass for solo developers managing GitHub workloads across multiple repositories.**

---

## What Is SoloDevBoard?

SoloDevBoard is a .NET 10 Blazor Server application designed for individual developers who work across multiple GitHub repositories. Rather than jumping between repository tabs, project boards, and settings pages, SoloDevBoard surfaces everything in one place — giving you a unified view of your issues, labels, workflows, and project health.

It is built with the solo developer in mind: opinionated defaults, minimal configuration, and AI-assisted workflows.

---

## Key Features

| Feature | Description |
|---------|-------------|
| **Audit Dashboard** | A consolidated view of issues, open PRs, label consistency, and workflow health across all your repositories. |
| **One-Click Migration** | Migrate labels, milestones, and project board configurations from one repository to another in a single action. |
| **Label Manager** | Create, edit, synchronise, and enforce label taxonomies across multiple repositories from a single interface. |
| **Board Rules Visualiser** | Visualise the automation rules configured on your GitHub project boards to understand how issues flow between columns. |
| **Triage UI** | A focused, keyboard-friendly interface for triaging incoming issues — apply labels, assign milestones, and close duplicates quickly. |
| **Workflow Templates** | Browse, apply, and customise GitHub Actions workflow templates across your repositories without leaving SoloDevBoard. |
| **Cross-Repo PM Workflow** | A Daily Focus view, cross-repository Backlog Review, and Iteration Planning tool — the UI-based evolution of the [github-workflows](https://github.com/markheydon/github-workflows) PM operating system. |

---

## Quick Links

- 📖 [Getting Started](getting-started.md) — prerequisites, local setup, and configuration
- 🧭 [Dashboard Shell Guide](user-guide/dashboard-shell.md) — navigate all six feature areas from the home page
- 🗂️ [Repositories Guide](user-guide/repositories.md) — view repositories available to your authenticated account
- 👤 [User Guide](user-guide/) — detailed guides for each feature
- 📅 [Cross-Repo PM Workflow](user-guide/pm-workflow.md) — daily focus, backlog review, and iteration planning
- 🏗️ [Infrastructure](../infra/README.md) — deploying to Azure with Bicep
- 📋 [Backlog](../plan/BACKLOG.md) — what's planned

---

## Project Status

> ⚠️ **Early Development** — SoloDevBoard is in active early development. Features are being built incrementally. See [plan/IMPLEMENTATION_PLAN.md](../plan/IMPLEMENTATION_PLAN.md) for the roadmap.

---

## Licence

SoloDevBoard is released under the [MIT Licence](../LICENSE).
