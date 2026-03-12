---
name: Execute Feature
description: Implements a planned GitHub issue following coding standards, creates tests, updates documentation, and ensures all mandatory gates are met. Invokes Delivery Agent.
agent: Delivery Agent
---

# Execute Feature Workflow

**When to use:** After planning completes (via `plan-next-issue` prompt), use this to implement the feature, write tests, and update documentation.

---

## Purpose

Execute the full implementation workflow for a planned issue:
1. Verify issue has clear acceptance criteria
2. Implement code following layered architecture
3. Create xUnit tests with proper coverage
4. Update user-facing documentation
5. Create ADR if architectural decision made
6. Synchronise backlog status

**Result:** Code, tests, and docs complete; ready for Review Agent to create PR and close issue.

---

## Inputs Required

Provide **ONE** of:
- **Issue number:** "Implement issue #31"
- **Feature name:** "Build Label Manager UI" (agent locates corresponding issue)
- **Story title:** "As a user, I can filter labels by type" (agent matches to issue)

---

## Workflow Steps

This prompt invokes the **Delivery Agent**, which executes:

### 1. Issue Context Verification
- Fetch issue details from GitHub
- Verify issue has:
  - Acceptance criteria defined
  - Labels applied (`type/`, `priority/`, `area/`, `size/`)
  - `status/todo` or `status/in-progress` label
  - Technical plan or breakdown in description
- **If issue not ready:** Escalate to PM Orchestrator for re-planning
- **Project board update:** Remove `status/todo` label, add `status/in-progress`; use `github-project` skill (Lifecycle Event 2) to set project Status → "In Progress" and **overwrite Start Date with today's actual start date** (not the original planned date — this is a required step, not optional)

### 2. Feature Branch Creation

Before any code is written, the Delivery Agent must create a dedicated feature branch from `main`:

```bash
git checkout main
git pull origin main
git checkout -b feature/issue-$issueNumber-brief-description
```

**Branch naming:** `feature/issue-N-kebab-case-description` (e.g. `feature/issue-31-label-manager-ui`).

All source code, tests, and documentation changes are committed to this branch. The branch reaches `main` only via a pull request created by the Review Agent — **never commit implementation code directly to `main`**.

### 3. Implementation Execution
- Follow layered architecture from `.github/copilot-instructions.md`:
  - **Domain** → no external dependencies
  - **Application** → depends on Domain only
  - **Infrastructure** → implements Application interfaces
  - **App (Blazor)** → calls Application layer
- Apply .NET 10 / C# 14 conventions per `.github/instructions/dotnet-framework.instructions.md`
- Use MudBlazor components per `.github/skills/mudblazor/SKILL.md` for UI work
- For Blazor UI work, prefer MudBlazor layout primitives and utility classes before introducing `.razor.css`; raw HTML and custom CSS need a clear justification
- **UK English only:** All comments, strings, user-facing text in UK English

### 4. Test Creation
- Add xUnit tests following `.github/skills/csharp-xunit/SKILL.md`
- Test naming: `MethodUnderTest_Scenario_ExpectedOutcome`
- Use Moq for mocking; xUnit built-in `Assert.*` for assertions — **FluentAssertions is prohibited** (see ADR-0008)
- Test projects mirror source project structure
- Arrange/Act/Assert sections separated by blank lines

### 5. Documentation Updates
- Update `docs/user-guide/*.md` if feature is user-facing
- Update `docs/index.md` quick links if new doc page added
- Add XML doc comments (`///`) to all public members

### 6. ADR Creation (if needed)
- Invoke `create-architectural-decision-record` skill if:
  - Architectural decision introduced during implementation
  - Design pattern chosen
  - Technology selection made
- Place ADR in `adr/` directory
- Update `adr/README.md` index

### 7. Backlog Synchronisation
- Update `plan/BACKLOG.md` to reflect implementation progress.
- Flag scope changes in `plan/SCOPE.md` for user review.

### 8. Self-Review (Pre-Handoff)

**Mandatory before marking implementation complete.** The Delivery Agent performs an explicit review pass over every file it has changed. This catches issues before the GitHub coding review agent sees the PR, avoiding a second implementation round-trip.

#### Automated checks
- `dotnet build` — zero errors, zero warnings.
- `dotnet test` — all tests passing.
- `get_errors` on each modified file — no diagnostics.

#### Source file checks
- Every `public` type, method, property, and constructor has a `///` XML doc comment.
- All comments, strings, and user-facing text use UK English (no `behavior`, `color`, `organize`, `center`, etc.).
- Every public constructor guards injected dependencies with `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace`.
- Every `await` in Application/Infrastructure uses `.ConfigureAwait(false)`; no sync-over-async (`.Result`, `.Wait()`).
- Public API methods return `IReadOnlyList<T>` / `IReadOnlyDictionary<TKey,TValue>`, not mutable collection types.
- All `.cs` files use file-scoped namespaces.
- No business logic in `.razor` files — logic lives in code-behind or Application layer.
- No layer boundary violations (no domain entities in Application service public signatures; no Infrastructure types in Razor components).
- MudBlazor-first UI: interactive controls and layout use MudBlazor components where available; avoid raw HTML except for framework-owned host elements or genuinely unsupported cases.
- Utility classes before CSS: use MudBlazor utility classes or component parameters for spacing, alignment, sizing, and visibility before adding or extending `.razor.css`.
- Any new custom CSS is minimal and justified because components, parameters, theming, and utility classes were insufficient.

#### Test file checks
- Every test method is named `MethodUnderTest_Scenario_ExpectedOutcome`.
- Arrange / Act / Assert blocks are each separated by a blank line.
- Only `Assert.*` from xUnit — no `FluentAssertions` (prohibited, ADR-0008).

**If any check fails:** fix before handing off. Do not pass known self-review findings to the Review Agent.

---

## Outputs Produced

### Artefacts Created/Modified
- **Source code** in `src/` following layered architecture
- **Test code** in `tests/` with full coverage
- **Documentation** in `docs/user-guide/` if user-facing
- **ADR** in `adr/` if architectural decision made

### Quality Gates Met
- ✅ Code follows .NET 10 / C# 14 conventions
- ✅ UK English used throughout
- ✅ Layered architecture rules enforced
- ✅ All public members have XML doc comments
- ✅ xUnit tests added with correct naming convention
- ✅ User-facing docs updated if applicable
- ✅ ADR created if architectural decision made
- ✅ No new compile errors or warnings

### Implementation Summary Delivered
```markdown
# Implementation Complete: Label Manager UI (Issue #31)

## Files Changed
### Source Code
- `src/App/SoloDevBoard.App/Components/Pages/LabelManager.razor` — new page component
- `src/App/SoloDevBoard.App/Components/Pages/LabelManager.razor.cs` — page code-behind
- `src/Application/SoloDevBoard.Application/Services/LabelService.cs` — new service
- `src/Infrastructure/SoloDevBoard.Infrastructure/GitHubService.cs` — added label CRUD methods

### Tests
- `tests/App.Tests/SoloDevBoard.App.Tests/LabelManagerTests.cs` — 8 new tests
- `tests/Application.Tests/SoloDevBoard.Application.Tests/LabelServiceTests.cs` — 12 new tests
- `tests/Infrastructure.Tests/SoloDevBoard.Infrastructure.Tests/GitHubServiceLabelTests.cs` — 6 new tests

### Documentation
- `docs/user-guide/label-manager.md` — new user guide page (updated)
- `docs/index.md` — added quick link to Label Manager guide

### Architecture
- `adr/0012-switch-to-mudblazor-component-library.md` — ADR for the MudBlazor component-library decision
- `adr/README.md` — updated index

## Test Coverage
- **26 tests added** (8 UI, 12 service, 6 integration)
- **All tests passing** ✅
- Coverage: UI components, service logic, GitHub API integration

## Quality Validation
- ✅ Zero compile errors/warnings
- ✅ UK English verified (colour, organise, behaviour)
- ✅ Layered architecture followed
- ✅ XML doc comments on all public members

## Next Action
✅ Ready for review — Use `review-and-close` prompt to create PR and close issue #31
```

---

## Agents/Skills Invoked

- **Delivery Agent** — full implementation orchestration
- **`dotnet-best-practices` skill** — code implementation
- **`mudblazor` skill** — UI components
- **`csharp-xunit` skill** — test creation
- **`csharp-docs` skill** — XML comment generation
- **`create-architectural-decision-record` skill** — ADR creation (if needed)
- **`documentation-writer` skill** — user guide updates

---

## Testing Phase — No Commits

Once implementation is delivered and the user begins testing, the Delivery Agent **must not commit or push** any changes until the user explicitly signals the session is complete.

### Recognising Testing Phase

The Delivery Agent enters Testing Phase when the user says (or implies) any of the following:
- "I'm testing this now"
- "Just spotted an issue"
- "Can you fix / tweak / adjust…"
- "While I'm testing…"
- "This isn't working quite right"
- Any correction or refinement whilst they are actively using or inspecting the delivered work

### Rules during Testing Phase

- Apply fixes directly to the working tree.
- Do **not** run `git commit` or `git push` after each individual fix.
- Confirm each fix verbally: "Fixed — not yet committed."
- Keep accumulating changes until the user signals acceptance.

### Exiting Testing Phase

The Delivery Agent commits when the user says (or implies):
- "Looks good"
- "All working"
- "Done testing"
- "Ready to commit"
- "Hand off to Review Agent"
- Or any clear signal that the testing session is over

On exit: stage all accumulated changes and create **one summary commit** covering the entire testing session, e.g.:

```
Fix iterative testing fixes for issue #68 — colour picker binding, dialog sizing, snackbar wording
```

> **Why one commit:** Multiple "fix X / fix Y" commits against a single issue misrepresent the implementation history and pollute the branch log. A single summary commit accurately reflects the work done.

---

## Follow-Up Prompts

After implementation completes:
- **To review and close:** Run `review-and-close.prompt.md` with the issue number
- **To address coding review comments on an open PR:** Run `address-pr-review-comments.prompt.md` with the PR number
- **To continue with next issue:** Run `plan-next-issue.prompt.md` to select next work
- **To validate locally:** Run `dotnet build` and `dotnet test` in terminal

---

## Example Invocations

### Example 1: Implement feature issue
**You say:**
```
Implement issue #31
```

**Agent executes:**
- Fetches issue #31 (Label Manager UI)
- Implements Blazor page with MudBlazor components
- Creates LabelService in Application layer
- Enhances GitHubService with label CRUD
- Adds 26 xUnit tests
- Updates user guide and index
- Creates ADR for UI component choice

**Output:**
```
Implemented Label Manager UI (issue #31). 4 source files, 3 test files, 2 doc files changed.
26 tests added, all passing. ADR-0007 created. Ready for review.
```

---

### Example 2: Implement bug fix
**You say:**
```
Fix bug #42
```

**Agent executes:**
- Fetches issue #42 (validation bug in Domain layer)
- Fixes Issue.cs validation logic
- Adds regression test in IssueTests.cs
- Updates backlog status

**Output:**
```
Fixed validation bug (issue #42). 1 source file, 1 test file changed.
Regression test added. Ready for review.
```

---

### Example 3: Implement infrastructure enabler
**You say:**
```
Implement issue #25
```

**Agent executes:**
- Fetches issue #25 (GitHub API client enabler)
- Creates GitHubService in Infrastructure layer
- Creates ADR for Octokit.NET selection
- Adds integration tests
- No user-facing docs (internal enabler)

**Output:**
```
Implemented GitHub API client (issue #25). Created ADR-0005 for Octokit.NET.
Integration tests added. Ready for review.
```

---

## Scope Escalation

If scope ambiguity discovered during implementation:
- Agent pauses and flags issue
- Recommends scope update in `plan/SCOPE.md`
- Waits for your decision before proceeding

**Example:**
```
⚠️ Scope Change Detected:
Implementation of "Label Manager UI" requires real-time label sync via webhooks.
This is not in plan/SCOPE.md.

Recommendation: Implement basic polling for Phase 1. Add webhooks to "Future Enhancements".

Options:
1. Proceed with polling (in-scope, simpler)
2. Add webhooks (scope change, requires replanning)
3. Pause and update SCOPE.md

Your choice?
```

---

## Mandatory Gates (Before Implementation Complete)

Implementation is NOT complete until:
- ✅ All acceptance criteria met.
- ✅ Code compiles without errors/warnings.
- ✅ Tests pass locally.
- ✅ Documentation updated (user-facing features only).
- ✅ ADR created (architectural decisions only).
- ✅ UK English verified throughout.
- ✅ Layered architecture rules followed.
- ✅ Backlog synchronised.
- ✅ **Self-review (Step 8) completed** — all source and test file checks passed with no outstanding findings.

**If any gate fails:** Agent escalates to you for resolution, does NOT proceed to review.

---

## Usage Tips

**Best for:**
- Planned features with clear acceptance criteria
- Stories/enablers from breakdown-plan output
- Work ready for implementation (not planning)

**Not for:**
- Unplanned work (run `plan-next-issue` first)
- Work missing acceptance criteria (escalate to PM Orchestrator)
- Exploratory spikes (use separate branch, manual coding)

---

## Quality Reminder

Before marking implementation complete, Delivery Agent validates:
- **Architecture:** Domain/Application/Infrastructure/App layers respected
- **Conventions:** .NET 10, C# 14, file-scoped namespaces, primary constructors
- **Testing:** xUnit, Moq, `Assert.*`, correct naming
- **Documentation:** XML comments, user guides, ADRs
- **Language:** UK English only (colour, organise, behaviour, licence, analyse, centre)
