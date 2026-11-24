> **Audience:** Maintainers and agents refreshing AI skill sources. Contributor how-to; not part of the published user site.

# Maintaining agent skills (Cursor plugins + in-repo)

Solo Dev Board keeps **project-authored** agent skills in git under [`.agents/skills/`](../.agents/skills/). **Aspire**, **.NET**, and **GitHub** capabilities for Cursor agents load from plugins enabled in [`.cursor/settings.json`](../.cursor/settings.json); those bundles are **not** copied into `.agents/skills/`.

**Typical maintainer requests:** bump plugin `gitRef` pins, configure the GitHub MCP plugin PAT, or regenerate the `dotnet-inspect` companion per this guide.

## What lives where

| Area | Location | Maintenance |
| --- | --- | --- |
| .NET plugin bundles (5) | Cursor plugins `dotnet-agent-skills/dotnet`, `dotnet-blazor`, `dotnet-test`, `dotnet-data`, `dotnet-nuget` in [`.cursor/settings.json`](../.cursor/settings.json) | Bump shared `gitRef` (commit or tag) from [dotnet/skills](https://github.com/dotnet/skills); reload Cursor |
| Aspire workflow bundle (8 skills) | Cursor plugin `aspire-skills/aspire` | Bump `gitRef` from [microsoft/aspire-skills](https://github.com/microsoft/aspire-skills); reload Cursor |
| GitHub MCP | Cursor plugin `github` (enabled only; marketplace-managed) | Personal access token in **Cursor → Plugins → GitHub** (see [GitHub Cursor plugin](#github-cursor-plugin-mcp)); not committed |
| `dotnet-inspect` companion | `.agents/skills/dotnet-inspect/` | Regenerate `SKILL.md` via `dnx dotnet-inspect` (below) |
| `playwright-cli` companion | **Not used** | See [Playwright: E2E vs agent CLI](#playwright-e2e-vs-agent-cli) |
| Project-authored skills | e.g. `mudblazor`, `code-review`, `implementation-guardrails` | Edit in-repo; never overwrite with upstream bundles |
| Spec Kit workflow skills | [`.cursor/skills/`](../.cursor/skills/) (`speckit-*`) | Managed by Spec Kit; independent of marketplace plugins |

Cursor also loads skills from [`.cursor/rules/skills-location.mdc`](../.cursor/rules/skills-location.mdc). Built-in Cursor skills live in `~/.cursor/skills-cursor/`; never commit or edit those.

Plugin skills are cached under the Cursor plugins directory (for example `~/.cursor/plugins/cache/dotnet-agent-skills/` or `.../aspire-skills/aspire/` on a dev machine). Agents should use the loaded plugin skill content, not assume paths under `.agents/skills/` for plugin bundles. Precedence when skills overlap is in [`AGENTS.md`](../AGENTS.md).

## When to run this

- After a release or commit of [dotnet/skills](https://github.com/dotnet/skills) you want to adopt (update the shared `gitRef` on all five enabled dotnet plugins).
- After a tagged release of [microsoft/aspire-skills](https://github.com/microsoft/aspire-skills) you want to adopt (update `gitRef` for `aspire-skills/aspire`).
- After upgrading **dotnet-inspect** when the embedded skill content has changed.
- When agent sessions show stale Aspire CLI or .NET test guidance relative to upstream skill content.
- When onboarding a contributor who needs the **GitHub** MCP plugin in Cursor (PAT setup).

You do **not** need to refresh plugin pins on every Aspire CLI or .NET SDK bump unless upstream release notes justify it.

---

## Update .NET workflow skills (Cursor plugin)

All five enabled plugins share one monorepo pin from [dotnet/skills](https://github.com/dotnet/skills).

**Configuration** (excerpt; keys must stay in sync):

```json
"plugins": {
  "dotnet-agent-skills/dotnet": {
    "enabled": true,
    "gitUrl": "https://github.com/dotnet/skills",
    "gitRef": "<commit-or-tag>"
  },
  "dotnet-agent-skills/dotnet-blazor": {
    "enabled": true,
    "gitUrl": "https://github.com/dotnet/skills",
    "gitRef": "<commit-or-tag>"
  },
  "dotnet-agent-skills/dotnet-test": {
    "enabled": true,
    "gitUrl": "https://github.com/dotnet/skills",
    "gitRef": "<commit-or-tag>"
  },
  "dotnet-agent-skills/dotnet-data": {
    "enabled": true,
    "gitUrl": "https://github.com/dotnet/skills",
    "gitRef": "<commit-or-tag>"
  },
  "dotnet-agent-skills/dotnet-nuget": {
    "enabled": true,
    "gitUrl": "https://github.com/dotnet/skills",
    "gitRef": "<commit-or-tag>"
  }
}
```

**Procedure:**

1. Choose an upstream **tag or commit** from [dotnet/skills](https://github.com/dotnet/skills) (record it in the PR or commit message).
2. Set the **same** `gitRef` on all five `dotnet-agent-skills/*` entries in `.cursor/settings.json`.
3. Commit the settings change only (no copies under `.agents/skills/` for plugin skills).
4. Reload the Cursor window or reopen the project so the plugin cache picks up the pin.

**Plugins enabled in this repository:**

| Plugin | Typical skills (upstream) |
| --- | --- |
| `dotnet` | `csharp-expert`, `csharp-refactoring`, `msbuild`, `dotnet-breaking-changes`, `setup-local-sdk` |
| `dotnet-blazor` | `author-component`, `use-js-interop`, `collect-user-input`, and related Blazor skills |
| `dotnet-test` | `run-tests`, `platform-detection`, `code-testing`, `test-gap-analysis`, and related test skills |
| `dotnet-data` | `optimizing-ef-core-queries`, `create-datadriven-aspnetcore` |
| `dotnet-nuget` | `convert-to-cpm` |

Repository-specific routing and precedence (for example `mudblazor` before generic Blazor skills) are in [`AGENTS.md`](../AGENTS.md).

---

## Update Aspire workflow skills (Cursor plugin)

**Configuration:**

```json
"plugins": {
  "aspire-skills/aspire": {
    "enabled": true,
    "gitUrl": "https://github.com/microsoft/aspire-skills",
    "gitRef": "<commit-or-tag>"
  }
}
```

**Procedure:**

1. Choose an upstream **tag or commit** from [microsoft/aspire-skills](https://github.com/microsoft/aspire-skills) (record it in the PR or commit message).
2. Set `gitRef` to that value in `.cursor/settings.json`.
3. Commit the settings change only (no copies under `.agents/skills/aspire*`).
4. Reload the Cursor window or reopen the project so the plugin cache picks up the pin.

**Skills in the bundle:**

| Skill | Role |
| --- | --- |
| `aspire` | Router and guardrails |
| `aspire-init` | `aspire new` / `aspire init` |
| `aspireify` | AppHost wiring after init |
| `aspire-orchestration` | Start, stop, lifecycle |
| `aspire-monitoring` | Logs, traces, dashboard |
| `aspire-deployment` | Publish and deploy |
| `aspire-project-v2-migration` | Project v2 migration |
| `aspire-terminals` | Terminals and automation |

### Aspire 17 and `aspire agent init`

- `aspire agent init --skills aspire` on 17+ registers plugin/catalog integration for GitHub Copilot, Claude Code, and OpenCode. For **Cursor**, this repository relies on [`.cursor/settings.json`](../.cursor/settings.json), not vendored copies under `.agents/skills/`.
- For **dotnet-inspect** only, an alternative is  
  `aspire agent init --non-interactive --agents none --skills dotnet-inspect --skill-locations standard --scope project`  
  This repository prefers the `dnx` skill output below for a single-file companion that matches the installed tool version.
- Do **not** use `aspire agent init` to install `playwright-cli` here; we do not use that skill.

### `aspire doctor` and duplicate copies

Do **not** re-vendor the eight Aspire skills under `.agents/skills/`. If `aspire doctor` suggests syncing skills into the repo, ignore that for Cursor when the plugin pin in `.cursor/settings.json` is present and enabled.

---

## GitHub Cursor plugin (MCP)

The **github** entry in [`.cursor/settings.json`](../.cursor/settings.json) enables Cursor's GitHub plugin. It exposes GitHub's remote MCP server; it does **not** ship agent `SKILL.md` files.

**Separate from application auth:** the AppHost parameter `gh-pat` and hosted sign-in described in [`docs/getting-started.md`](getting-started.md) authenticate **Solo Dev Board** against GitHub. The Cursor plugin PAT authenticates **the agent in your IDE** only.

**Setup:**

1. Ensure `"github": { "enabled": true }` in `.cursor/settings.json` (already committed for this repo).
2. In Cursor, open **Settings → Plugins → GitHub** and set **GitHub personal access token** to a fine-grained or classic PAT from [GitHub token settings](https://github.com/settings/tokens).
3. Grant only scopes the agent needs (typical: repository contents, issues, pull requests, metadata; Actions if CI inspection is required).
4. Never commit the token. Rotate from GitHub Settings if it is exposed.

**Agent routing:** prefer `gh` for pull requests, issues, and releases per [`AGENTS.md`](../AGENTS.md). Use the GitHub MCP plugin when richer API or search access is needed in chat.

**References:**

- [Use the GitHub MCP server](https://docs.github.com/en/copilot/how-tos/context/use-mcp/use-the-github-mcp-server)
- [Cursor plugins repository](https://github.com/cursor/plugins) (`cursor-public/github`)

---

## Refresh `dotnet-inspect`

The companion skill teaches the [dotnet-inspect](https://github.com/richlander/dotnet-inspect) CLI. Vendoring the markdown is separate from installing the tool.

**Prerequisite:** `dotnet-inspect` available via `dnx`, for example:

```bash
dnx dotnet-inspect -y -- --help
```

If that fails, install or update the tool per upstream documentation, then rerun the skill command.

**Regenerate `SKILL.md`:**

```bash
mkdir -p .agents/skills/dotnet-inspect
dnx dotnet-inspect -y -- skill > .agents/skills/dotnet-inspect/SKILL.md
```

The command should print the full skill body (YAML frontmatter including `version:`). Commit the updated file. Using the skill in agent sessions still requires `dnx dotnet-inspect` (or an equivalent install) on the machine running the agent.

**Optional upstream check:** compare with  
`https://github.com/richlander/dotnet-inspect/blob/main/skills/dotnet-inspect/SKILL.md`  
if the `skill` subcommand output format changes between releases.

---

## Playwright: E2E vs agent CLI

Two different Playwright surfaces exist. Solo Dev Board uses only the **.NET** stack for tests.

| Thing | Role in this repository |
| --- | --- |
| `Microsoft.Playwright.Xunit.v3` and `playwright.ps1 install chromium` | Local and CI end-to-end tests ([`tests/SoloDevBoard.E2E.Tests/README.md`](../tests/SoloDevBoard.E2E.Tests/README.md), [`scripts/test-e2e.sh`](../scripts/test-e2e.sh)) |
| npm `@playwright/cli` (`playwright-cli`) and a local `playwright-cli` skill | **Out of scope** unless explicitly adopted |

We **do not** vendor or enable `.agents/skills/playwright-cli/`. Do **not** add npm or `playwright-cli install --skills` to the dev container or skill maintenance flow solely to update that skill.

The upstream **aspire-monitoring** skill (via the plugin) may reference browser handoff guidance. That does **not** require the npm CLI or a local `playwright-cli` skill. Without them, agents should use `aspire describe` for endpoints and the E2E project or documented manual checks for browser verification.

**If you ever adopt agent-driven browser automation via `playwright-cli`:**

1. Add it through Cursor plugin or project skill placement per upstream [microsoft/playwright-cli](https://github.com/microsoft/playwright-cli) documentation (not routine in this repo today).
2. Installing the CLI for real use remains a separate runtime choice (npm global or project tool).

---

## Verification checklist

After a refresh:

- [ ] `.cursor/settings.json` has the intended shared `gitRef` and `enabled: true` on all five `dotnet-agent-skills/*` plugins.
- [ ] `.cursor/settings.json` has the intended `gitRef` and `enabled: true` for `aspire-skills/aspire`.
- [ ] `github` remains `enabled: true` (no secret values in git).
- [ ] No new or restored paths under `.agents/skills/aspire*` or vendored copies of marketplace .NET plugin skills.
- [ ] Each touched project skill directory still contains `SKILL.md` with valid frontmatter where applicable.
- [ ] No accidental changes under project-authored skills (`mudblazor`, `code-review`, Spec Kit paths, etc.).
- [ ] No new files under `.cursor/skills-cursor/` or wholesale replacement of `.cursor/skills/speckit-*`.

---

## What agents must not modify during skill maintenance

| Path | Reason |
| --- | --- |
| `.cursor/skills/speckit-*` | Spec Kit workflow; not upstream marketplace bundles |
| `.agents/skills/mudblazor`, `code-review`, `implementation-guardrails`, `project-documentation`, `create-architectural-decision-record` | Authored for Solo Dev Board |
| `~/.cursor/skills-cursor/` | Cursor-managed built-ins |
| `.agents/skills/playwright-cli/` | Not used in this repository |
| `.agents/skills/aspire*` or vendored dotnet plugin skills | Use plugin pins instead; do not reintroduce copies |

## See also

- [`AGENTS.md`](../AGENTS.md) - task routing, precedence, and skills index
- [dotnet/skills](https://github.com/dotnet/skills)
- [Aspire skills (official)](https://aspire.dev/get-started/aspire-skills/)
- [`docs/getting-started.md`](getting-started.md) - local setup, dev containers, Codespaces, and application GitHub auth