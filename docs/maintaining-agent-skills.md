> **Audience:** Maintainers and agents refreshing vendored AI skills. Contributor how-to; not part of the published user site.

# Maintaining project agent skills (Aspire 17 + Cursor)

Solo Dev Board commits agent skills under [`.agents/skills/`](../.agents/skills/) so Cursor, Cloud Agents, and contributors share the same guidance. From **Aspire 17**, `aspire agent init` registers plugins for GitHub Copilot, Claude Code, and OpenCode; it does **not** populate project skill folders for **Cursor**. Vendoring in git is intentional for this repository.

**Typical agent request:** refresh vendored agent skills per this guide.

## What lives where

| Area | Path | Maintenance |
| --- | --- | --- |
| Aspire workflow bundle (8 skills) | `.agents/skills/aspire*` | Sync from [microsoft/aspire-skills](https://github.com/microsoft/aspire-skills) `skills/` |
| `dotnet-inspect` companion | `.agents/skills/dotnet-inspect/` | Regenerate `SKILL.md` via `dnx dotnet-inspect` (below) |
| `playwright-cli` companion | **Not vendored** | See [Playwright: E2E vs agent CLI](#playwright-e2e-vs-agent-cli) |
| Project-authored skills | e.g. `mudblazor`, `code-review`, `implementation-guardrails` | **Do not** overwrite when syncing upstream bundles |
| Spec Kit workflow skills | [`.cursor/skills/`](../.cursor/skills/) (`speckit-*`) | Managed by Spec Kit; do not mix with aspire-skills sync |

Cursor also loads skills from [`.cursor/rules/skills-location.mdc`](../.cursor/rules/skills-location.mdc). Built-in Cursor skills live in `~/.cursor/skills-cursor/`; never commit or edit those.

## When to run this

- After a tagged release of [microsoft/aspire-skills](https://github.com/microsoft/aspire-skills) you want to adopt.
- After upgrading **dotnet-inspect** and the embedded skill content has changed.
- When agent sessions show stale Aspire CLI behaviour or broken links in vendored skills.

You do **not** need to refresh skills on every Aspire CLI bump unless the aspire-skills release notes justify it.

---

## Sync Aspire workflow skills

**Source:** `https://github.com/microsoft/aspire-skills/tree/main/skills` (or a release tag). Copy **only** each skill subtree under `skills/<name>/`. Do **not** clone the whole repository into `.agents/skills/`, and do **not** rely on `.cursor-plugin/` marketplace metadata for this repo.

**Folders to sync (eight):**

| Skill directory | Role |
| --- | --- |
| `aspire` | Router and guardrails |
| `aspire-init` | `aspire new` / `aspire init` |
| `aspireify` | AppHost wiring after init |
| `aspire-orchestration` | Start, stop, lifecycle |
| `aspire-monitoring` | Logs, traces, dashboard |
| `aspire-deployment` | Publish and deploy |
| `aspire-project-v2-migration` | Project v2 migration |
| `aspire-terminals` | Terminals and automation |

### Procedure

1. Choose an upstream **tag or commit** (record it in the PR or commit message).
2. For each name above, copy the matching tree from upstream `skills/<name>/` into `.agents/skills/<name>/`, replacing upstream files.
3. **Preserve** any project-only skills in `.agents/skills/` that are not in that list (never delete `mudblazor`, `code-review`, etc.).
4. Commit the diff; no code or package changes are required for a pure skill sync.

Example (from a temporary clone; adjust tag and paths):

```bash
UPSTREAM=/path/to/aspire-skills
TAG=v0.0.6   # example only
for name in aspire aspire-init aspireify aspire-orchestration aspire-monitoring \
  aspire-deployment aspire-project-v2-migration aspire-terminals; do
  rsync -a --delete "$UPSTREAM/skills/$name/" ".agents/skills/$name/"
done
```

Use `rsync --delete` only on the eight upstream mirrors, not on `.agents/skills/` as a whole.

### Aspire 17 and `aspire agent init`

- `aspire agent init --skills aspire` on 17+ enables plugin/catalog **registration** for Copilot, Claude, and OpenCode. It does **not** replace vendoring for Cursor.
- For **dotnet-inspect** only, an alternative is  
  `aspire agent init --non-interactive --agents none --skills dotnet-inspect --skill-locations standard --scope project`  
  This repository prefers the `dnx` skill output below for a single-file companion that matches the installed tool version.
- Do **not** use `aspire agent init` to install `playwright-cli` here; we do not vendor that skill.

### `aspire doctor` warnings

Aspire may report vendored Aspire skill copies as stale relative to the plugin model. For this repository, committed copies under `.agents/skills/` are **intentional** for Cursor and project scope. Keep them unless you deliberately move to per-machine plugin install only.

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
| npm `@playwright/cli` (`playwright-cli`) and `.agents/skills/playwright-cli/` | **Out of scope** unless explicitly adopted |

We **do not** vendor `.agents/skills/playwright-cli/`. Do **not** add npm or `playwright-cli install --skills` to the dev container or skill maintenance flow solely to update that skill.

The synced **aspire-monitoring** skill still includes upstream [`playwright-handoff.md`](../.agents/skills/aspire-monitoring/references/playwright-handoff.md) (discover the frontend URL with Aspire before browser work). That reference does **not** require the npm CLI or a local `playwright-cli` skill. Without them, agents should use `aspire describe` for endpoints and the E2E project or documented manual checks for browser verification.

**If you ever adopt agent-driven browser automation via `playwright-cli`:**

1. Copy `skills/playwright-cli/` (including `references/`) from [microsoft/playwright-cli](https://github.com/microsoft/playwright-cli) at a **tagged release**, into `.agents/skills/playwright-cli/`, using the same git vendoring model as aspire-skills (not `playwright-cli install --skills`).
2. Installing the CLI for real use remains a separate runtime choice (npm global or project tool); it is not part of routine skill refresh in this repo.

---

## Verification checklist

After a refresh:

- [ ] Each updated directory contains `SKILL.md` with valid frontmatter (`name`, `description`; `version` where upstream provides it).
- [ ] No accidental changes under project-authored skills (`mudblazor`, `code-review`, Spec Kit paths, etc.).
- [ ] No new files under `.cursor/skills-cursor/` or wholesale replacement of `.cursor/skills/speckit-*`.
- [ ] Optional: spot-check `.agents/skills/aspire-init/references/init-workflow.md` for Cursor / `skillSources` behaviour still matching how this repo works.

---

## What agents must not modify during skill sync

| Path | Reason |
| --- | --- |
| `.cursor/skills/speckit-*` | Spec Kit workflow; not upstream Aspire |
| `.agents/skills/mudblazor`, `code-review`, `implementation-guardrails`, `project-documentation`, `create-architectural-decision-record` | Authored for Solo Dev Board |
| `~/.cursor/skills-cursor/` | Cursor-managed built-ins |
| `.agents/skills/playwright-cli/` | Not used in this repository |

## See also

- [`AGENTS.md`](../AGENTS.md) - task routing and skills index
- [Aspire skills (official)](https://aspire.dev/get-started/aspire-skills/)
- [`plan/CURSOR_CLOUD.md`](../plan/CURSOR_CLOUD.md) - Cloud VM and dev container notes