# SoloDevBoard end-user documentation (Hugo)

This directory is the Hugo site that publishes SoloDevBoard's **end-user** documentation to GitHub Pages using the [Hextra](https://imfing.github.io/hextra/) theme.

Developer and operator documentation lives in [`docs/`](../docs/) and is not published by this site.

## Local preview

Requires Podman (default) or Docker. No local Hugo or Go install is needed.

```powershell
# Live-reload development server (http://localhost:1313)
.\scripts\Invoke-HugoSite.ps1 serve

# One-shot production-style build to user-docs/public
.\scripts\Invoke-HugoSite.ps1 build

# Build then serve the static output with nginx (http://localhost:8080)
.\scripts\Invoke-HugoSite.ps1 preview
```

## Deployment

GitHub Actions workflows:

- `.github/workflows/hugo-ci.yml` — build validation on pull requests.
- `.github/workflows/hugo-deploy.yml` — build and deploy to GitHub Pages on `main`.
- `.github/workflows/hugo-build.yml` — reusable build/deploy job.

See [DEC-019](../plan/DECISIONS.md#dec-019-hugo-hextra-for-end-user-docs-on-github-pages) and [DOCS_STRATEGY.md](../plan/DOCS_STRATEGY.md).

## Screenshots

Feature screenshots live under `static/images/<feature-slug>/` and are referenced from guide pages as `/images/<feature-slug>/<name>.png`.

Capture conventions (light theme, 1400×900, docs capture mode, kebab-case filenames) are documented in [DOCS_STRATEGY.md](../plan/DOCS_STRATEGY.md#screenshot-convention). To regenerate:

```powershell
# App running locally with a real PAT and DocsCapture:Enabled=true
cd tests/E2E
npm run capture:docs
```
