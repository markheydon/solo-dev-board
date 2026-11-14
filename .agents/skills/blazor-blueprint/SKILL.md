---
name: blazor-blueprint
description: >-
  Use BlazorBlueprint (Bb*) APIs from official machine-readable documentation.
  Fetch docs on demand via llms.txt. Use when implementing or refactoring
  BlazorBlueprint UI, theming, overlays, icons, or layout utilities.
---

# BlazorBlueprint

Official docs are published as plain text under [blazorblueprintui.com/llms.txt](https://blazorblueprintui.com/llms.txt). **Do not vendor `llms-full.txt` into the repo** (large, changes with releases). **Do not load the full bundle into context** unless the task is a rare cross-cutting audit.

## Workflow

1. Open `https://blazorblueprintui.com/llms.txt` for entry points and topic paths.
2. Resolve a component or topic via `https://blazorblueprintui.com/llms/index.txt` (search for `Bb*` name or keyword).
3. Fetch **only** the files you need (WebFetch or `curl -sL`), for example `setup.txt`, `patterns.txt`, `services.txt`, or one `components/{slug}.txt` / `primitives/{slug}.txt`.
4. Use parameter names, child content, and service wiring from the fetched doc when writing Razor.
5. If the doc and compiler disagree, trust the build and the installed package version.

## URL index

| Need | URL |
|------|-----|
| Entry / package list | `https://blazorblueprintui.com/llms.txt` |
| Component name → doc path | `https://blazorblueprintui.com/llms/index.txt` |
| Install, render modes, `bb:` Tailwind prefix | `https://blazorblueprintui.com/llms/setup.txt` |
| `@bind`, controlled state, conventions | `https://blazorblueprintui.com/llms/patterns.txt` |
| Toast, dialog, theme, portal services | `https://blazorblueprintui.com/llms/services.txt` |
| Styled component | `https://blazorblueprintui.com/llms/components/{slug}.txt` |
| Headless primitive | `https://blazorblueprintui.com/llms/primitives/{slug}.txt` |
| Ready-made compositions | `https://blazorblueprintui.com/llms/blueprints.txt` |

Slug is kebab-case from the index (e.g. `BbDataTable` → `data-table.txt`).

## Fetch hints

| Task | Typical files |
|------|----------------|
| Global setup or first package use | `setup.txt` |
| Dialogs, sheets, toasts, theme service | `services.txt` + one component doc |
| Odd binding or state | `patterns.txt` |
| Unknown `Bb*` API | `index.txt` → single `components/{slug}.txt` |
| Copy from catalog | `blueprints.txt` then path under `blueprints/` |

Avoid `llms-full.txt` and bulk-fetching every `components/*.txt` unless you are editing many unrelated components in one task.
