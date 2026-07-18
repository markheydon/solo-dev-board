# Implement Issue

Implement a planned GitHub issue after PM planning is complete.

Use this command in Cursor Agent chat after GitHub issues exist with acceptance criteria. Planning, board updates, and review/PR workflows can be run separately via workflow prompts or natural language.

---

## Authoritative contract

Read and follow [`.agents/contracts/delivery.md`](.agents/contracts/delivery.md) in full for all implementation steps, boundaries, escalation rules, and completion criteria.

This command adds Cursor-specific issue loading, skill loading, and platform split notes only.

---

## Issue input

The user provides an issue number or feature name (for example `184`, `#184`, or `Implement issue #184`).

Before coding:

1. Run `gh issue view <N> --json title,body,state,labels` to load the issue.
2. Confirm the issue exists, is open, and has acceptance criteria or implementation notes.
3. Assume issues created via Copilot planning workflows are implementation-ready unless a clearly missing prerequisite is discovered.

For multiple related issues (for example `#100` and `#101`), implement them on the same feature branch when appropriate.

---

## Readiness gates

From [AGENTS.md`](AGENTS.md) and the Delivery Agent:

- Do not start coding before planning and issue creation are complete.
- For page-producing UI work, do not start implementation until a wireframe exists in `plan/wireframes/` and is referenced by the issue or planning artefacts.
- Escalate to the user (not PM Orchestrator) if acceptance criteria are missing, scope is unclear, or an architectural choice needs approval.

---

## Feature branch

Confirm you are not on `main` before editing.

If no feature branch exists, create one using:

```text
feature/issue-N-description
```

Examples:

```text
feature/issue-184-board-rules-diagram
feature/issue-110-label-manager
```

Never implement directly on `main`.

---

## Skills to load

Read these skill files explicitly before and during implementation:

| When | Skill |
|------|-------|
| Always | [`.agents/skills/dotnet-best-practices/SKILL.md`](.agents/skills/dotnet-best-practices/SKILL.md) |
| Blazor UI work | [`.agents/skills/mudblazor/SKILL.md`](.agents/skills/mudblazor/SKILL.md) |
| Tests | [`.agents/skills/csharp-xunit/SKILL.md`](.agents/skills/csharp-xunit/SKILL.md) |
| Public API XML docs | [`.agents/skills/csharp-docs/SKILL.md`](.agents/skills/csharp-docs/SKILL.md) |
| Architectural decision | [`.agents/skills/create-architectural-decision-record/SKILL.md`](.agents/skills/create-architectural-decision-record/SKILL.md) |
| User-facing documentation | [`.agents/skills/documentation-writer/SKILL.md`](.agents/skills/documentation-writer/SKILL.md) |

Path-scoped rules in `.cursor/rules/` also point at [`.github/instructions/blazor.instructions.md`](.github/instructions/blazor.instructions.md) and [`.github/instructions/dotnet-framework.instructions.md`](.github/instructions/dotnet-framework.instructions.md) when editing matching files.

---

## Boundaries

Do **not**:

- Create pull requests.
- Close issues.
- Update GitHub project board fields, issue status labels, milestones, or roadmap dates.
- Update `plan/SCOPE.md` without user approval.
- Implement unplanned scope.

**Platform split:** Planning and board operations are separate workflow steps. Cursor owns code, tests, documentation, and `plan/BACKLOG.md` synchronisation for this command.

---

## Self-review

Before handoff, run:

```bash
dotnet build
dotnet test
```

Confirm build succeeds, tests pass, and changed files follow repository architecture and conventions. Review only files changed by this implementation.

---

## Commit policy

When implementation is complete (before the testing phase), commit work to the feature branch per the Delivery Agent completion criteria.

During the **testing phase** (see below), do not commit or push until the user signals acceptance.

---

## Expected outcomes

- Feature branch created or selected.
- Implementation completed.
- Tests added or updated.
- Relevant documentation updated.
- `plan/BACKLOG.md` updated.
- Ready for review.

---

## Testing phase

If the user begins testing the implementation:

- Remain on the same branch.
- Do not commit.
- Do not push.
- Apply fixes directly to the working tree.
- Confirm: `Fixed — not yet committed.`
- Accumulate fixes during the session.
- Create one summary commit when testing is complete.

Remain in testing phase until the user signals acceptance (for example: "Looks good", "Ready to commit", "Done testing", "Hand off to review").

Example summary commit message:

```text
Fix testing feedback for issue #184 - diagram labels, layout spacing, error wording
```

---

## Output contract

When implementation is complete, provide:

```text
Implementation Complete

Issue: #123

Files Changed:
- file1
- file2

Tests:
- 5 new tests
- all passing

Documentation:
- user guide updated

Backlog:
- updated

Ready for Review Agent.
```

---

## Next step

After implementation and testing are complete, hand off to review:

```text
Review issue #<N>
```

Use the [`.github/prompts/review-and-create-pr.prompt.md`](.github/prompts/review-and-create-pr.prompt.md) workflow (Review contract) to validate the work and create a pull request.
