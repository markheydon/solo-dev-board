---
weight: 90
title: Workflow Templates
---

## Overview

The Workflow Templates feature allows you to browse, apply, and customise GitHub Actions workflow templates across your repositories, all from within SoloDevBoard. Rather than manually copying YAML files between repositories, you can manage templates centrally and push them where needed.

Key goals of Workflow Templates:
- Provide a library of reusable GitHub Actions workflow templates suited to common .NET development patterns.
- Allow templates to be parameterised and customised per repository before being applied.
- Track which repositories have a given template applied and whether they are up to date.

---

## How to Use

### Browse built-in templates

1. Open **Workflow Templates** from the navigation menu or the home dashboard.
2. Select one or more target repositories using the repository selector at the top of the page.
3. Review the built-in template cards for common .NET workflows such as CI, Azure CD (Aspire), and Dependabot auto-merge.
4. Use the search field to filter templates by name, category, or tag.
5. Select a category chip (for example **CI** or **CD**) to narrow the list.
6. Select a template card to open the detail panel.

Each template card shows:

- Template name and description.
- Category and tags.
- Target workflow file path.
- Trigger summary describing when the workflow runs.

### Customise template parameters

1. With a template selected, review the YAML preview in the detail panel.
2. Adjust parameter fields such as branch names, .NET versions, or deployment environment names.
3. Required parameters must be completed before the apply action is enabled.

Parameter values are used to render the workflow file that will be written to each target repository.

### Apply a template to repositories

1. Select the repositories that should receive the workflow file.
2. Confirm parameter values and review repository status badges in the detail panel.
3. Select **Apply template to selected repositories**.

The feedback region shows per-repository results, including created, updated, skipped, and failed outcomes. Partial failures are reported clearly so you can retry affected repositories.

### Review repository status and drift

When a template and repositories are selected, the detail panel shows workflow status for each repository:

- **Not applied** â€” the workflow file is missing.
- **Applied** â€” the workflow file matches the rendered template.
- **Drifted** â€” the workflow file exists but differs from the canonical template.

Drift detection is informational and does not block browsing or previewing templates. Review drift warnings before applying if you need to preserve local customisations.

---

## Configuration

Planned configuration options include:
- Defining custom template repositories to supplement the built-in library.
- Configuring default parameter values for templates (for example default .NET version or default branch name).
