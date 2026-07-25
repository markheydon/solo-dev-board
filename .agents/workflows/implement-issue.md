# Implement Issue

**Contract:** [`.agents/contracts/delivery.md`](../contracts/delivery.md)
**Runbook:** [`plan/PM_RUNBOOK.md`](../../plan/PM_RUNBOOK.md) — Stage 2: Implementation
**Skills (on demand):** `dotnet-best-practices`, `mudblazor`, `csharp-xunit`, `csharp-docs`, `repo-decision-log`, `documentation-writer`

## Easy-to-miss specifics

- **Preflight is mandatory** and runs before the feature branch — see Implementation Preflight in the Delivery contract.
- Load the issue first: `gh issue view <N> --json title,body,state,labels` (labels are required for the proceed gate).
- Parse the `## Implementation References` section from the issue body.
- For UI work, read the linked wireframe from [`plan/wireframes/`](../../plan/wireframes/).
- Invoke `dotnet-best-practices` during preflight for all issues; invoke `mudblazor` for UI issues.
- Apply the tiered proceed gate: auto-continue for `size/xs`, `size/s`, and `type/bug`; pause for `size/m+` and all `type/enabler` issues.
- Consult other skills when relevant — do not require reading every skill up front.

### Area label → codebase hints

| `area/` label | Primary paths |
|---------------|---------------|
| `area/labels` | `src/App/.../Features/Labels/`, `src/Application/.../Services/Labels/`, `tests/` |
| `area/dashboard` | `src/App/.../Features/Audit/`, `src/Application/.../Services/Audit/` |
| `area/migration` | `src/App/.../Features/Migration/`, `src/Application/.../Services/Migration/` |
| `area/triage` | `src/App/.../Features/Triage/`, `src/Application/.../Services/Triage/` |
| `area/board-rules` | `src/App/.../Features/BoardRules/`, `src/Application/.../Services/BoardRules/` |
| `area/workflows` | `src/App/.../Features/Workflows/`, `src/Application/.../Services/Workflows/` |
| `area/infrastructure` | `src/Infrastructure/`, `src/SoloDevBoard.AppHost/` |
| `area/docs` | `docs/`, `plan/` |

## Invocation

Natural language: "Implement issue #N"
