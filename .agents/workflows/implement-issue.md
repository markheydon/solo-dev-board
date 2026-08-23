# Implement Issue

**Contract:** [`.agents/contracts/delivery.md`](../contracts/delivery.md)
**Runbook:** [`plan/PM_RUNBOOK.md`](../../plan/PM_RUNBOOK.md) — Stage 2: Implementation
**Skills (on demand):** `dotnet-best-practices`, `mudblazor`, `csharp-xunit`, `csharp-docs`, `repo-decision-log`, `documentation-writer`
**Skills (required):** `aspire` (router), then `aspire-orchestration` and `aspire-monitoring` when running or diagnosing the AppHost

## Easy-to-miss specifics

- **Preflight is mandatory** and runs before the feature branch — see Implementation Preflight in the Delivery contract.
- Load the issue first: `gh issue view <N> --json title,body,state,labels` (labels are required for the proceed gate).
- Parse the `## Implementation References` section from the issue body.
- For UI work, read the linked wireframe from [`plan/wireframes/`](../../plan/wireframes/).
- Invoke `dotnet-best-practices` during preflight for all issues; invoke `mudblazor` for UI issues.
- This repo is Aspire-hosted. Read the `aspire` skill at the start of implementation. After C# or Razor fixes while the AppHost is running, `aspire resource app rebuild` then `aspire wait app`. On exceptions, use `aspire otel logs app` and `aspire logs app`.
- Apply the tiered proceed gate: auto-continue for `size/xs`, `size/s`, and `type/bug`; pause for `size/m+` and all `type/enabler` issues.
- After the proceed gate, set `status/in-progress` on the issue (see Mark Work Started in the Delivery contract) before creating the feature branch.
- Do not implement issues with `status/blocked` or `status/ice-box` — escalate or re-queue first.
- Do not set `status/in-progress` during standalone `/preflight-issue`.
- Consult other skills when relevant — do not require reading every skill up front, except the required Aspire skills above.
- If this session started from a GitHub Issue comment that only says implement / fix / do **this issue**, follow [deliver-issue](deliver-issue.md) instead (open a PR). Use this split workflow only from chat `/implement-issue`, or when the comment explicitly says not to open a pull request.

### Area label → codebase hints

| `area/` label | Primary paths |
|---------------|---------------|
| `area/labels` | `src/App/.../Features/Labels/`, `src/Application/.../Services/Labels/`, `tests/` |
| `area/dashboard` | `src/App/.../Features/Audit/`, `src/Application/.../Services/Audit/` |
| `area/migration` | `src/App/.../Features/Migration/`, `src/Application/.../Services/Migration/` |
| `area/triage` | `src/App/.../Features/Triage/`, `src/Application/.../Services/Triage/` |
| `area/board-rules` | `src/App/.../Features/BoardRules/`, `src/Application/.../Services/BoardRules/` |
| `area/workflows` | `src/App/.../Features/Workflows/`, `src/Application/.../Services/Workflows/` |
| `area/actions-templates` | `src/App/.../Features/Workflows/`, `src/Application/.../Services/Workflows/` |
| `area/planning` | `src/App/.../Features/PmWorkflow/`, `src/Application/.../Services/PmWorkflow/` |
| `area/infrastructure` | `src/Infrastructure/`, `src/SoloDevBoard.AppHost/` |
| `area/docs` | `website/`, `docs/`, `plan/` |

## Invocation

**Chat:** "Implement issue #N".
**Slash command:** `/implement-issue [number]`.
**GitHub Issue comment** (mention at the start; `N` is this issue):
- `@cursor implement this issue without opening a PR`
- `@cursor implement only — do not create a pull request`

Bare `@cursor implement this issue` is **not** this workflow. Route it to [deliver-issue](deliver-issue.md).
