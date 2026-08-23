---
weight: 70
title: Actions Templates
landing: true
landingIcon: account_tree
landingSubtitle: "Browse, customise, and apply GitHub Actions workflow templates across repositories."
guideStatus: Partially Available
---

## Overview

The Actions Templates feature allows you to browse, apply, and customise GitHub Actions workflow templates across your repositories, all from within SoloDevBoard. Rather than manually copying YAML files between repositories, you can manage templates centrally and push them where needed.

![Actions Templates with a selected template YAML preview for markheydon/solo-dev-board](/images/workflow-templates/overview.png)

Key goals of Actions Templates:
- Provide a library of reusable GitHub Actions workflow templates suited to common .NET development patterns.
- Allow templates to be parameterised and customised per repository before being applied.
- Track which repositories have a given template applied and whether they are up to date.

---

## How to Use

{{% steps %}}

### Browse built-in templates

1. Open **Actions Templates** from the navigation menu or the app home page.
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

- **Not applied** — the workflow file is missing.
- **Applied** — the workflow file matches the rendered template.
- **Drifted** — the workflow file exists but differs from the canonical template.

Drift detection is informational and does not block browsing or previewing templates. Review drift warnings before applying if you need to preserve local customisations.

{{% /steps %}}

### What is coming later

- Custom template repositories so you can maintain organisation-specific templates outside the built-in catalogue.
- Persisted default parameter profiles so common values can be reused across apply runs without re-entering them each time.

---

## Configuration

{{< callout type="info" >}}
**Scope note** — Built-in templates, parameterisation, apply, and drift detection are available now. Custom template repositories and persisted default parameter profiles are planned for a later release.
{{< /callout >}}

Current behaviour:

- Templates are provided as a built-in catalogue.
- Parameter values are entered per apply run in the UI.
- There are no `appsettings.json` entries specific to this feature.
