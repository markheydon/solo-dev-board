# Product site About / history wireframe

## Purpose

- Explain where SoloDevBoard came from and how the project is run.
- Keep copy factual and maintainable; no commercial or hype claims.

## User goals

- Learn the motivation (single pane of glass for GitHub workloads across repos).
- Understand lineage from the maintainer's GitHub workflow/PM tooling.
- Read an honest account of AI-collaborator-driven planning and delivery.

## About section (`/about/`)

```
+------------------------------------------------------------------+
| SoloDevBoard    About   User Guide   [Search]   GitHub   [theme] |
+------------------------------------------------------------------+
| About SoloDevBoard                                               |
|                                                                  |
| Sidebar / section nav (Hextra docs layout):                      |
|   - Origin                                                       |
|   - How the project is run                                       |
+------------------------------------------------------------------+
| ## Origin                                                        |
| Single pane of glass for solo developers...                      |
| Inspired by markheydon/github-workflows PM operating system.     |
|                                                                  |
| ## How the project is run                                        |
| Began as an AI-fully-controlled experiment: planning artefacts,  |
| issues, wireframes, and implementation in-repo with AI agents.   |
| What that means in practice (honest, not marketing).             |
+------------------------------------------------------------------+
```

### Pages

| Route | Title | Content |
|-------|-------|---------|
| `/about/` | About SoloDevBoard | Section index with links to child pages |
| `/about/origin/` | Origin | Vision from SCOPE; github-workflows lineage |
| `/about/how-we-work/` | How the project is run | AI collaborator experiment; repo PM workflow |

### Interaction notes

- Use Hextra docs section layout (not `hextra-home`).
- Distinct from in-app **About** page documented at `/docs/about/` (app version metadata).
- No claims about paid features, donations, staging-for-donation, or Marketplace.
- Link to GitHub repo for contribution; operator setup stays in repo `docs/`.

### Accessibility

- Standard heading hierarchy (H1 page title, H2 sections).
- Internal links use descriptive text.
