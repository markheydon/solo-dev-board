# Agent Instructions

Repository-specific operating rules for AI coding agents.

## Core Context

Read before non-trivial changes:

- [`GOALS.md`](GOALS.md)
- [`SCOPE.md`](SCOPE.md) (summary; detail in [`plan/SCOPE.md`](plan/SCOPE.md))
- [`CONVENTIONS.md`](CONVENTIONS.md)
- [`adr/`](adr/) - accepted decisions

## Language

UK English in documentation, comments, and user-facing text. Do not use the em dash (U+2014); see [`CONVENTIONS.md`](CONVENTIONS.md) (Documentation).

## Tech Stack

- .NET 10.0 (LTS), Blazor Server, C# 14.
- xUnit v3 with Microsoft.Testing.Platform; NSubstitute when mocks are required; built-in asserts only.
- MudBlazor for UI; Aspire AppHost for local orchestration and Azure Container Apps deployment.

## Skills

Project skills live in [`.agents/skills/`](.agents/skills/).

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
- **Documentation** (`**/*.md` except `adr/`): `project-documentation` skill.
- **ADRs** (`adr/*.md`): ADR skill only.

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