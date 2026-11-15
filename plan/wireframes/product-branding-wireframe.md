# Product branding chrome

**Status:** Implemented with [#397](https://github.com/markheydon/solo-dev-board/issues/397); shipped [`v1.4.0`](https://github.com/markheydon/solo-dev-board/releases/tag/v1.4.0). Bundle: [`archive/product-chrome-ui-quality/README.md`](../archive/product-chrome-ui-quality/README.md).

## Naming split

| Layer | Value |
| --- | --- |
| Spoken / wordmark / docs | **Solo Dev Board** (`ProductBranding.DisplayName`) |
| Solution, namespaces, C# identifiers | `SoloDevBoard` |
| Repository slug | `solo-dev-board` |
| Public domain | `solodevboard.com` |
| Label Manager strategy id | `solodevboard` |
| Label Manager strategy display name | Solo Dev Board |
| GitHub App listing **name** | `SoloDevBoard` (uniqueness); description uses Solo Dev Board |
| HTTP User-Agent product token | `SoloDevBoard/...` |

## Surfaces that share one asset set

```
Browser tab     [favicon.svg]  Solo Dev Board
App bar         [32px mark] Solo Dev Board
Welcome         [72px mark] Solo Dev Board
Apple touch     favicon.png / icon-512.png
GitHub App      docs/assets/github-app/icon.svg + icon-512.png
Public site     website/static/images/logo.svg (Hextra displayLogo)
```

Master art: Solo family seal (primary green field, three board columns, solid solo focus disc on the middle column). Copies live under `wwwroot` and `website/static/images` because each host needs its own static root; geometry and `#167c38` fill stay in sync via tests.

## App bar (with mark)

```
[menu] [logo 32] Solo Dev Board                    [theme] [more]
```

The mark is `BrandMark` (`MudImage` of `favicon.svg`), decorative when the wordmark is visible (`alt=""`).

## Out of scope here

- Page body visual language (see [product-ui-language-wireframe.md](product-ui-language-wireframe.md)).
- Redesigning the public site IA.

## Accessibility

- Favicon and app-bar mark are decorative when the word Solo Dev Board is visible; `alt=""`.
- Touch icon must remain distinguishable at 180px.
