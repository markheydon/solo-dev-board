> **Audience:** Maintainers and agents refreshing AI skill sources. Contributor how-to; not part of the published user site.

# Maintaining project agent skills (Aspire 17 + Cursor)

Solo Dev Board keeps **project-authored** agent skills in git under [`.agents/skills/`](../.agents/skills/). The **Aspire workflow bundle** (eight skills) is **not** copied into that tree; Cursor loads it from the [microsoft/aspire-skills](https://github.com/microsoft/aspire-skills) plugin pinned in [`.cursor/settings.json`](../.cursor/settings.json) (Aspire 17 marketplace model).

**Typical agent request:** bump the aspire-skills plugin pin or regenerate the `dotnet-inspect` companion per this guide.

## What lives where

| Area | Location | Maintenance |
| --- | --- | --- |
| Aspire workflow bundle (8 skills) | Cursor plugin `aspire-skills/aspire` in [`.cursor/settings.json`](../.cursor/settings.json) | Bump `gitRef` (commit or tag) from [microsoft/aspire-skills](https://github.com/microsoft/aspire-skills); reopen or reload Cursor so the plugin cache refreshes |
| `dotnet-inspect` companion | `.agents/skills/dotnet-inspect/` | Regenerate `SKILL.md` via `dnx dotnet-inspect` (below) |
| `playwright-cli` companion | **Not used** | See [Playwright: E2E vs agent CLI](#playwright-e2e-vs-agent-cli) |
| Project-authored skills | e.g. `mudblazor`, `code-review`, `implementation-guardrails` | Edit in-repo; never overwrite with upstream bundles |
| Spec Kit workflow skills | [`.cursor/skills/`](../.cursor/skills/) (`speckit-*`) | Managed by Spec Kit; independent of aspire-skills |

Cursor also loads skills from [`.cursor/rules/skills-location.mdc`](../.cursor/rules/skills-location.mdc). Built-in Cursor skills live in `~/.cursor/skills-cursor/`; never commit or edit those.

Plugin skills are cached under the Cursor plugins directory (for example `~/.cursor/plugins/cache/aspire-skills/aspire/` on a dev machine). Agents should use the loaded plugin skill content, not assume paths under `.agents/skills/aspire*`.

## When to run this

- After a tagged release of [microsoft/aspire-skills](https://github.com/microsoft/aspire-skills) you want to adopt (update `gitRef` in `.cursor/settings.json`).
- After upgrading **dotnet-inspect** when the embedded skill content has changed.
- When agent sessions show stale Aspire CLI behaviour relative to upstream skill guidance.

You do **not** need to refresh the plugin pin on every Aspire CLI bump unless the aspire-skills release notes justify it.

---

## Update Aspire workflow skills (Cursor plugin)

**Configuration:** [`.cursor/settings.json`](../.cursor/settings.json) enables the plugin and pins upstream:

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

- [ ] `.cursor/settings.json` has the intended `gitRef` and `enabled: true` for `aspire-skills/aspire`.
- [ ] No new or restored paths under `.agents/skills/aspire*` (Aspire bundle stays plugin-only).
- [ ] Each touched project skill directory still contains `SKILL.md` with valid frontmatter where applicable.
- [ ] No accidental changes under project-authored skills (`mudblazor`, `code-review`, Spec Kit paths, etc.).
- [ ] No new files under `.cursor/skills-cursor/` or wholesale replacement of `.cursor/skills/speckit-*`.

---

## What agents must not modify during skill maintenance

| Path | Reason |
| --- | --- |
| `.cursor/skills/speckit-*` | Spec Kit workflow; not upstream Aspire |
| `.agents/skills/mudblazor`, `code-review`, `implementation-guardrails`, `project-documentation`, `create-architectural-decision-record` | Authored for Solo Dev Board |
| `~/.cursor/skills-cursor/` | Cursor-managed built-ins |
| `.agents/skills/playwright-cli/` | Not used in this repository |
| `.agents/skills/aspire*` | Use plugin pin instead; do not reintroduce vendored copies |

## See also

- [`AGENTS.md`](../AGENTS.md) - task routing and skills index
- [Aspire skills (official)](https://aspire.dev/get-started/aspire-skills/)
- [`plan/CURSOR_CLOUD.md`](../plan/CURSOR_CLOUD.md) - Cloud VM and dev container notes