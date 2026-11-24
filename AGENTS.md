# Agent Instructions

Repository-specific operating rules for AI coding agents.

## Core Context

Read before non-trivial changes:

- [`GOALS.md`](GOALS.md)
- [`SCOPE.md`](SCOPE.md) (summary; detail in [`plan/SCOPE.md`](plan/SCOPE.md))
- [`CONVENTIONS.md`](CONVENTIONS.md)
- [`adr/`](adr/) - accepted decisions
- [`.specify/memory/constitution.md`](.specify/memory/constitution.md) - when doing Spec Kit feature work (principles and quality gates)

## Language

UK English in documentation, comments, and user-facing text. Do not use the em dash (U+2014); see [`CONVENTIONS.md`](CONVENTIONS.md) (Documentation).

## Tech Stack

- .NET 10.0 (LTS), Blazor Server, C# 14.
- xUnit v3 with Microsoft.Testing.Platform; NSubstitute when mocks are required; built-in asserts only.
- MudBlazor for UI; Aspire AppHost for local orchestration and Azure Container Apps deployment.

## Skills

### Configuration inventory

[`.cursor/settings.json`](.cursor/settings.json) is the enablement list for Cursor plugins. Plugin skills load from the Cursor plugin cache (for example `~/.cursor/plugins/cache/...` on a dev machine), **not** from `.agents/skills/`.

| Source | Upstream | In this repo |
| --- | --- | --- |
| Project skills | [`.agents/skills/`](.agents/skills/) | Authored in git (see table below) |
| .NET plugin bundles | [dotnet/skills](https://github.com/dotnet/skills) | `dotnet`, `dotnet-blazor`, `dotnet-test`, `dotnet-data`, `dotnet-nuget` |
| Aspire workflow bundle | [microsoft/aspire-skills](https://github.com/microsoft/aspire-skills) | `aspire-skills/aspire` (eight skills) |
| GitHub integration | [GitHub MCP](https://docs.github.com/en/copilot/how-tos/context/use-mcp/use-the-github-mcp-server) via Cursor **github** plugin | MCP tools only (no `SKILL.md`) |
| Spec Kit | [`.cursor/skills/`](.cursor/skills/) (`speckit-*`) | Invoke via `/speckit-specify`, `/speckit-plan`, and other `/speckit-*` commands |
| `dotnet-inspect` companion | [richlander/dotnet-inspect](https://github.com/richlander/dotnet-inspect) | Vendored [`.agents/skills/dotnet-inspect/`](.agents/skills/dotnet-inspect/) |

Bump plugin `gitRef` values per [`docs/maintaining-agent-skills.md`](docs/maintaining-agent-skills.md). Full plugin catalogues: [dotnet/skills README](https://github.com/dotnet/skills), [microsoft/aspire-skills](https://github.com/microsoft/aspire-skills). Official Aspire product reference: [aspire.dev](https://aspire.dev/).

### Project skills

| Skill | Use when |
| --- | --- |
| `create-architectural-decision-record` | Creating or major-updating an ADR |
| `project-documentation` | Diátaxis and project-aware docs (README, `docs/`, placement) |
| `code-review` | PR or branch review before merge (see [`.cursor/commands/loop-code-review.md`](.cursor/commands/loop-code-review.md)) |
| `implementation-guardrails` | Non-trivial implementation: architecture fit, proportionality, root-cause fixes, build/test discipline |
| `mudblazor` | Blazor UI pages and components (MudBlazor patterns) |
| `dotnet-inspect` | Evidence-first .NET package and API investigation |

### Precedence (repository guidance wins)

When a plugin skill overlaps project rules, follow this order.

| Topic | Use first | Plugin skill only when |
| --- | --- | --- |
| Blazor / MudBlazor UI | `mudblazor` | Generic component architecture (`author-component`, `use-js-interop`) **after** mudblazor constraints |
| Implementation / architecture | `implementation-guardrails` + ADRs | `csharp-refactoring`, `csharp-expert`, `msbuild` for mechanical or build-specific work |
| Pre-merge review | `code-review` | — |
| Tests | `CONVENTIONS.md`, [`.cursor/rules/testing.mdc`](.cursor/rules/testing.mdc), test commands below | `run-tests`, `platform-detection`, `code-testing`, `test-gap-analysis`, and related **dotnet-test** skills; not `writing-mstest-tests` |
| NuGet / CPM | Explicit user approval in this file | `convert-to-cpm` when maintaining `Directory.Packages.props` |
| EF Core | ADRs + conventions | `optimizing-ef-core-queries` |
| Aspire / AppHost | `aspire` plugin router + `dotnet-inspect` | — |

### Curated plugin routing

Repo-relevant plugin skills only; see upstream READMEs for the full skill lists.

| Skill or bundle | Use when |
| --- | --- |
| `run-tests` | Choosing correct `dotnet test` / MTP flags for this solution |
| `platform-detection` | Uncertain test platform before running or filtering |
| `code-testing` | Writing or repairing xUnit tests (respect repo assert and mock rules) |
| `test-gap-analysis` | Checking whether tests would catch a production change |
| `csharp-refactoring` | Behaviour-preserving refactors (pair with `dotnet-breaking-changes` if public API) |
| `msbuild` | Build failures, targets, binlogs (not routine feature work) |
| `convert-to-cpm` | Central package management changes |
| `optimizing-ef-core-queries` | EF performance review |
| `aspire`, `aspire-init`, `aspireify`, `aspire-orchestration`, `aspire-monitoring`, `aspire-deployment`, `aspire-project-v2-migration`, `aspire-terminals` | AppHost wiring, CLI lifecycle, logs, deploy, terminals |
| GitHub **plugin** (MCP) | GitHub API, search, or Actions via MCP when `gh` is insufficient |

### GitHub plugin vs `gh`

- **Pull requests, issues, and releases in this workflow:** use `gh` per [`plan/PULL_REQUEST_POLICY.md`](plan/PULL_REQUEST_POLICY.md).
- **GitHub MCP plugin:** optional; set a personal access token in **Cursor → Plugins → GitHub** (separate from AppHost `gh-pat`; see [`docs/maintaining-agent-skills.md`](docs/maintaining-agent-skills.md)). Use for rich GitHub API exploration in agent sessions. Do not commit tokens.

## Task Routing

- **Code** (`src/`, `tests/`): [`CONVENTIONS.md`](CONVENTIONS.md).
- **New product work:** Spec Kit (`specs/<feature>/`) and the [constitution](.specify/memory/constitution.md). Product code still follows `CONVENTIONS.md` and `implementation-guardrails`. Do not mint Epic → Feature → Story trees from planning prompts ([`plan/SPEC_KIT_MIGRATION.md`](plan/SPEC_KIT_MIGRATION.md)).
- **Shipped increments** (including `v1.4` chrome and branding): historical issues and [`plan/archive/`](plan/archive/README.md). Do not reopen those bundles or mint new children unless the user explicitly asks.
- **Documentation** (`**/*.md` except `adr/`): `project-documentation` skill.
- **ADRs** (`adr/*.md`): ADR skill only. Record the decision. Do not append release status to an accepted ADR.

## Spec-driven features (Spec Kit)

Feature work lives under `specs/<feature>/`. **`spec.md`** is the feature-intent authority for product and acceptance intent; **`plan.md`, `tasks.md`,** and other delivery derivations are subordinate records for that increment and MUST NOT be kept in sync with every post-ship tweak once delivery completes. See [`.specify/memory/constitution.md`](.specify/memory/constitution.md) (Post-delivery persistence).

Shipped pre-Spec Kit increments stay in GitHub history and [`plan/archive/`](plan/archive/README.md). Do not start a Spec Kit pass for them unless the user asks for a post-ship archival spec. Resolve the active feature directory from [`.specify/feature.json`](.specify/feature.json) or:

```bash
.specify/scripts/bash/check-prerequisites.sh --json --require-tasks --include-tasks
```

Typical flow (analyze and converge are optional):

```text
/speckit-constitution → /speckit-specify → /speckit-plan → /speckit-tasks
  → (optional) do-analyse-after-tasks → do-implement → (optional) do-converge-after-implement
  → (optional) close-spec-kit-feature → (optional) loop-code-review
```

| Cursor command | Role |
| --- | --- |
| [do-analyse-after-tasks.md](.cursor/commands/do-analyse-after-tasks.md) | Looped pre-implementation audit of spec/plan/tasks (`/speckit-analyze`) |
| [do-implement.md](.cursor/commands/do-implement.md) | Implement `tasks.md` with branch, build/test, and PR conventions |
| [do-converge-after-implement.md](.cursor/commands/do-converge-after-implement.md) | Looped post-implement gap pass (`/speckit-converge` + follow-up tasks) |
| [close-spec-kit-feature.md](.cursor/commands/close-spec-kit-feature.md) | Mark the increment delivered in `spec.md` when all tasks are done; report doc and ADR gaps without rewriting delivery artefacts |
| [loop-code-review.md](.cursor/commands/loop-code-review.md) | Looped merge-readiness review of the branch diff |

Do **not** edit `.specify/` templates, integration manifests, or Spec Kit skill files unless the user explicitly asks. Editing files under the active `specs/<feature>/` directory is normal during specify, plan, tasks, analyze, and converge.

## Not Allowed Without Explicit Instruction

- Add or remove NuGet packages (except central versions in `Directory.Packages.props` when maintaining deps).
- Modify CI/CD behaviour.
- Commit secrets or API keys.

## Pull Request Workflow

Follow [`plan/PULL_REQUEST_POLICY.md`](plan/PULL_REQUEST_POLICY.md) and [`.github/pull_request_template.md`](.github/pull_request_template.md).

Before opening a PR:

```bash
dotnet format SoloDevBoard.slnx --verify-no-changes
dotnet build SoloDevBoard.slnx -c Release -warnaserror
dotnet test SoloDevBoard.UnitTests.slnf -c Release --no-build
```

Title shape: `[Type] Imperative summary (#NNN)` with labels from [`plan/LABEL_STRATEGY.md`](plan/LABEL_STRATEGY.md).

All agent-authored pull requests require human review before merge.

## Release checklist

When asked to cut a release or tag, follow the [release runbook](plan/RELEASE_PLAN.md#release-runbook) in full. Do not tag while the User Guide or screenshots are still outstanding. That pass includes every guide page affected by the release, a full recapture when shared chrome, navigation, theme, or the product name changed, and any guide or screenshot debt left by the previous tag. GitHub Pages publishes only from the `v*` tag, so the guide updates must be on `main` first.

## Environment-specific guidance

Dev Containers, Codespaces, Aspire orchestration, and AppHost parameters: [`docs/getting-started.md`](docs/getting-started.md).