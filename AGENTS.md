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

**Project skills** live in [`.agents/skills/`](.agents/skills/). **Spec Kit workflow skills** live in [`.cursor/skills/`](.cursor/skills/) (`speckit-*`); invoke them via `/speckit-specify`, `/speckit-plan`, and the other `/speckit-*` commands.

| Skill | Use when |
| --- | --- |
| `create-architectural-decision-record` | Creating or major-updating an ADR |
| `project-documentation` | Diátaxis and project-aware docs (README, `docs/`, placement) |
| `code-review` | PR or branch review before merge (see [`.cursor/commands/loop-code-review.md`](.cursor/commands/loop-code-review.md)) |
| `implementation-guardrails` | Non-trivial implementation: architecture fit, proportionality, root-cause fixes, build/test discipline |
| `mudblazor` | Blazor UI pages and components (MudBlazor patterns) |

Onboard **.NET Aspire** using current official documentation (not copied Aspire skills from this repository).

## Task Routing

- **Code** (`src/`, `tests/`): [`CONVENTIONS.md`](CONVENTIONS.md).
- **`v1.4` / `v1.5` product chrome** ([#526](https://github.com/markheydon/solo-dev-board/issues/526) tree): normal Plan mode and [`plan/product-chrome-ui-quality/`](plan/product-chrome-ui-quality/README.md); **do not** use Spec Kit as the planning gate; **do not** mint new child issues via planning prompts ([`plan/SPEC_KIT_MIGRATION.md`](plan/SPEC_KIT_MIGRATION.md)).
- **Spec Kit features** (`specs/<feature>/`): default for **new work after `v1.5.0`**; Spec Kit workflow and [constitution](.specify/memory/constitution.md); product code still follows `CONVENTIONS.md` and `implementation-guardrails`.
- **Documentation** (`**/*.md` except `adr/`): `project-documentation` skill.
- **ADRs** (`adr/*.md`): ADR skill only.

## Spec-driven features (Spec Kit)

Feature intent lives under `specs/<feature>/` (`spec.md`, `plan.md`, `tasks.md`, and related artefacts). **Not used for `v1.4` or `v1.5` delivery** unless the user explicitly overrides. Resolve the active feature directory from [`.specify/feature.json`](.specify/feature.json) or:

```bash
.specify/scripts/bash/check-prerequisites.sh --json --require-tasks --include-tasks
```

Typical flow (analyze and converge are optional):

```text
/speckit-constitution → /speckit-specify → /speckit-plan → /speckit-tasks
  → (optional) do-analyse-after-tasks → do-implement → (optional) do-converge-after-implement
  → (optional) loop-code-review
```

| Cursor command | Role |
| --- | --- |
| [do-analyse-after-tasks.md](.cursor/commands/do-analyse-after-tasks.md) | Looped pre-implementation audit of spec/plan/tasks (`/speckit-analyze`) |
| [do-implement.md](.cursor/commands/do-implement.md) | Implement `tasks.md` with branch, build/test, and PR conventions |
| [do-converge-after-implement.md](.cursor/commands/do-converge-after-implement.md) | Looped post-implement gap pass (`/speckit-converge` + follow-up tasks) |
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

## Environment-specific guidance

For Cursor Cloud VM development (Aspire lifecycle, secrets mapping, MCP, HTTPS caveats), see [`plan/CURSOR_CLOUD.md`](plan/CURSOR_CLOUD.md). Standard local setup remains in [`docs/getting-started.md`](docs/getting-started.md).