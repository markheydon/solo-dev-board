# Product branding chrome

**Status:** Planning baseline for ice-box feature [#397](https://github.com/markheydon/solo-dev-board/issues/397).  
**Related:** interim favicon from PR [#398](https://github.com/markheydon/solo-dev-board/pull/398); styling audit on the issue.

Do not implement until brand scope is agreed. This wireframe only shows **where** a mark would live.

## Surfaces that must share one asset set

```
Browser tab     [favicon.svg]  SoloDevBoard
App bar         [32px mark] SoloDevBoard
Apple touch     icon-512 equivalent
GitHub App      listing icon (same SVG)
Public site     optional wordmark in header (Hextra displayLogo)
```

Until a dedicated logo is chosen, keep the bar-chart GitHub App mark from `docs/assets/github-app/`. Duplicate SVG copies in `wwwroot` and `docs/assets` should become one source.

## App bar (with mark)

```
[menu] [logo 32] SoloDevBoard                    [theme] [more]
```

The mark is `MudImage` or inline SVG styled with theme primary, not a second competing colour.

## Out of scope here

- Page body visual language (see [product-ui-language-wireframe.md](product-ui-language-wireframe.md)).
- Redesigning the public site IA.

## Accessibility

- Favicon and app-bar mark are decorative when the word SoloDevBoard is visible; `alt=""`.
- Touch icon must remain distinguishable at 180px.
