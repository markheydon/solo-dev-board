# Implementation guardrails - reference checklists

Read when applying [../SKILL.md](../SKILL.md) to structural changes, diagnostic fixes, or EF work.

## Proportionality red flags

Treat these as **guilty until proven necessary**:

- Project that always changes with another project.
- One interface / one implementation "for DI".
- Repository or unit-of-work that only wraps `DbContext`/`DbSet`.
- Service that only forwards to another service.
- Manager/provider/handler/coordinator adding no behaviour.
- Duplicate DTO/entity models without a real boundary.
- MediatR/CQRS pipeline for straightforward CRUD.
- Premature multi-provider or plugin extensibility.
- Circular or awkward dependencies from over-decomposition.
- Ceremonial "Clean Architecture" layers with no enforced rules.

For each, document answers to the seven proportionality questions in [implementation-depth.md](implementation-depth.md).

## Diagnostic integrity

When fixing warnings or CI failures, search the change (and shared props) for:

| Mechanism | Notes |
| --- | --- |
| `NoWarn`, `WarningsNotAsErrors` | Project or Directory.Build - often too broad |
| `#pragma warning disable` / `restore` | Must name diagnostic ID and rationale |
| `SuppressMessage`, `GlobalSuppressions.cs` | Prefer fix at source |
| Nullable disable / `#nullable disable` | Whole-file disable is a last resort |
| `.editorconfig` `dotnet_diagnostic.*` | Severity reductions hide issues |
| `EnforceCodeStyleInBuild=false` | Weakens repo standards |
| Excluded files/projects/analyzers | Must be minimal |
| `[Fact(Skip=…)]`, disabled tests | Fix or delete with cause |
| Coverage exclusions | Must not hide production logic |

**Classify each material suppression**

- Justified (document why).
- Should be narrowed.
- Should be removed.
- Requires clarification.

Flag any change whose main effect is making a diagnostic disappear.

## EF Core migrations

```
Is migration history already applied in production/shared environments?
├── Unknown → Do not recommend squash/rewrite; ask human
├── Yes → Treat as immutable; forward-only fixes; plan data impact
└── No / disposable dev DB only
    ├── Churny pre-release history (fix/rename cycles)?
    │   └── Propose consolidation only with explicit user OK + recreate DBs
    └── Clean or single baseline → Keep; avoid empty/near-empty migrations
```

Never instruct squashing without stating the assumption that affected databases may be recreated.

Distinguish:

- Poor schema design.
- Messy dev-only migration history.
- Legitimate production history.
- Dangerous production history (needs strategy, not casual edits).

## Pre-PR self-review

- [ ] Read constraints ([SKILL.md](../SKILL.md) step 1)
- [ ] Code sits in the correct layer with correct dependency direction
- [ ] No new structure without proportionality answers
- [ ] No forbidden default fixes ([implementation-depth.md](implementation-depth.md))
- [ ] No agent-shaped shortcuts ([implementation-depth.md](implementation-depth.md))
- [ ] Tests assert behaviour; none weakened or skipped for convenience
- [ ] EF/migration changes respect lifecycle (this file, EF section)
- [ ] Validation commands run; failures not masked
- [ ] Documented architecture matches change or docs updated deliberately
- [ ] Justified existing boundaries left intact ([implementation-depth.md](implementation-depth.md))
