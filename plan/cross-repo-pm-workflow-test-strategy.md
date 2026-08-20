# Cross-Repo PM Workflow — test strategy

## Scope

Validate feature [#272](https://github.com/markheydon/solo-dev-board/issues/272) against `plan/wireframes/pm-workflow-wireframe.md` and `website/content/docs/pm-workflow.md`.

Layers:

| Layer | Where | What |
|-------|--------|------|
| Unit | `tests/Application` (and Domain if records need tests) | Ranking, stall detection, grouping, capacity, exclusion filter, label helpers |
| Component | `tests/App` bUnit | Tab shell, empty/error/loading, disabled Planning add when stalled |
| E2E | `tests/E2E/tests/pm-workflow.spec.ts` | Nav, four routes, no-board and load-failure shells (CI placeholder auth) |
| Docs-capture | `tests/E2E/docs-capture/` | Public-only screenshots after UI exists (DEC-020) |

Do not test AppHost orchestration.

## Quality objectives

- Stall and capacity rules are deterministic and unit-tested (3-day and 14-day boundaries inclusive).
- UI never hard-codes Project #8 field ids.
- Playwright CI does not require a live PAT; it asserts shell copy aligned with the user guide.
- UK English in assertions (`colour` not required here; “organisation” not used).

## Risks

| Risk | Test |
|------|------|
| Wrong stall timestamp | Unit tests with injected clock and Status-changed-at vs `updatedAt` fallback |
| Excluded repo still in recommendations | Unit: catalogue filter; bUnit: summary table |
| Planning write without resolving stall | bUnit: add button disabled |
| Hosted inaccessible board | bUnit/E2E: warning text already used by Triage |
| Guide published while behaviour is incomplete | Keep `draft: true` until the four tabs match the guide and E2E mapping lands in the same change set as publish |

## Coverage by area

### Enablers

- GraphQL mapping: items, Status name, Focus Order, content number/repo.
- Settings round-trip fake storage.
- Work items: label parse, priority rank, blocked/ice-box exclusion, issue vs PR.

### Daily Focus (#273–#276)

- Column counts from fixture boards with extra Status names.
- Active load = Up Next + In Progress.
- Top 3 ranking table.
- Stall inclusive of day 3.

### Backlog (#277–#282)

- Group membership decision table (urgent, ready, triage, parked, neglected).
- Epic complete: `subIssues.total > 0` and `completed == total`.
- Chip for Issue vs PR.

### Planning (#283–#286)

- Focus Order assignment skips Feature/Epic.
- Capacity dialog when exceeding limit.
- Milestone skip list for repos without that milestone.

### Repo Management (#287–#288)

- Exclude/include persistence.
- Counts ignore excluded repos.

## E2E alignment

When the guide is published, add a row to `tests/E2E/USER_DOCS_ALIGNMENT.md`:

| Guide | Routes | Spec | CI |
|-------|--------|------|-----|
| `pm-workflow.md` | `/pm-workflow`, `/pm-workflow/daily-focus`, `/pm-workflow/backlog`, `/pm-workflow/planning`, `/pm-workflow/repos` | `pm-workflow.spec.ts` | Tier 2 shell |

Update `tests/E2E/CRITICAL_JOURNEYS.md` with the morning-focus and planning-session journeys.

## Definition of done (tests)

- [ ] Four `type/test` issues closed with the paired stories.
- [ ] xUnit naming `MethodUnderTest_Scenario_ExpectedOutcome`.
- [ ] NSubstitute + `Assert.*` only.
- [ ] Alignment docs updated in the PR that publishes the user guide.
