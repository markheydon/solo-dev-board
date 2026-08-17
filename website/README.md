# SoloDevBoard public product site (Hugo)

This directory is the Hugo site that publishes SoloDevBoard's **public product site** to GitHub Pages using the [Hextra](https://imfing.github.io/hextra/) theme:

- `/` — product landing
- `/docs/` — User Guide (in-app features)
- `/about/` — project origin and how the repository is run

Developer and operator documentation lives in [`docs/`](../docs/) and is not published by this site.

## Local preview

Requires Podman (default) or Docker. No local Hugo or Go install is needed.

```bash
# Live-reload development server (http://localhost:1313)
./scripts/invoke-hugo-site.sh serve

# One-shot production-style build to website/public
./scripts/invoke-hugo-site.sh build

# Build then serve the static output with nginx (http://localhost:8080)
./scripts/invoke-hugo-site.sh preview
```

On Windows PowerShell, the same commands are available via `.\scripts\Invoke-HugoSite.ps1`.

## Deployment

GitHub Actions workflows:

- `.github/workflows/hugo-ci.yml` — build validation on pull requests.
- `.github/workflows/hugo-deploy.yml` — build and deploy to GitHub Pages on `v*` release tags only (DEC-021).

See [DEC-019](../plan/DECISIONS.md#dec-019-hugo-hextra-for-end-user-docs-on-github-pages), [DEC-021](../plan/DECISIONS.md#dec-021-two-tier-cd-pipeline), and [DEC-023](../plan/DECISIONS.md#dec-023-public-product-site-ia-and-canonical-domain).

## Custom domain (`solodevboard.com`)

The site is configured for apex domain `https://solodevboard.com/` via `hugo.yaml` `baseURL` and `static/CNAME`.

**Operator steps (before the first `v*` tag after merging domain changes):**

1. In the repository **Settings → Pages**, set the custom domain to `solodevboard.com` and wait for DNS verification.
2. At your DNS provider, add the records GitHub Pages documents for apex hosting (typically `A`/`AAAA` to GitHub Pages IPs, or follow GitHub's current guidance for apex domains). Optionally add `www` as a `CNAME` to `<user>.github.io` if you want `www.solodevboard.com`.
3. Enable **Enforce HTTPS** once the certificate is issued.
4. Retire or redirect `https://markheydon.me.uk/solo-dev-board/` on that host (this repository cannot redirect a path on another domain).

Do not push a release tag until DNS resolves and Pages shows a healthy custom domain, or visitors may see errors until propagation completes.

## Screenshots

Feature screenshots live under `static/images/<feature-slug>/` and are referenced from guide pages as `/images/<feature-slug>/<name>.png`.

Capture conventions (light theme, 1400×900, docs capture mode, kebab-case filenames, loaded-state composition with `markheydon/solo-dev-board`) are documented in [DOCS_STRATEGY.md](../plan/DOCS_STRATEGY.md#screenshot-convention). To regenerate:

```powershell
# App running locally with a real PAT and DocsCapture:Enabled=true
cd tests/E2E
npm run capture:docs
```
