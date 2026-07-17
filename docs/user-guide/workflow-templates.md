---
layout: page
title: Workflow Templates
parent: User Guide
nav_order: 6
---

> ⚠️ **Partially Available** — Template browsing is available. Customisation, apply, and staleness tracking are planned for later Phase 4 stories.

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
2. Review the built-in template cards for common .NET workflows such as CI, Azure CD (Aspire), and Dependabot auto-merge.
3. Use the search field to filter templates by name, category, or tag.
4. Select a category chip (for example **CI** or **CD**) to narrow the list.
5. Select a template card to highlight it before customising or applying the template in a later workflow.

Each template card shows:

- Template name and description.
- Category and tags.
- Target workflow file path.
- Trigger summary describing when the workflow runs.

### Coming soon

The following interactions are planned for later Phase 4 stories:
- Customising template parameters before applying a template to a repository.
- Applying a template to one or more repositories.
- Viewing which repositories already have a template applied and whether it differs from the canonical version.
- Updating an out-of-date workflow file across multiple repositories.

---

## Configuration

*Coming soon — this section will describe configuration options for Workflow Templates.*

Planned configuration options include:
- Defining custom template repositories to supplement the built-in library.
- Configuring default parameter values for templates (e.g. default .NET version, default branch name).
