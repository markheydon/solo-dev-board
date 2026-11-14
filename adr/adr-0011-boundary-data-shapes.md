# ADR-0011: Boundary Data Shapes — DTOs at the Application→App Boundary

**Date:** 2026-03-08
**Status:** Accepted

---

## Context

[ADR-0004](0004-layered-architecture.md) defines the four-layer architecture and establishes which projects may depend on which. It is explicit on **dependency direction** (Application depends on Domain; App depends on Application) but entirely silent on **what data shapes cross each boundary**. This gap is architecturally significant: without a rule, any code — human or AI-generated — may pass domain entities all the way through to the Blazor UI layer, which violates the original intent of Clean Architecture and accumulates coupling over time.

The question was surfaced during implementation of `ILabelManagerService`, which in its initial stub form returns `IReadOnlyList<Label>` — a domain entity type — to the `App` layer. Given that SoloDevBoard is developed primarily by AI delivery agents (see [ADR-0010](0010-bunit-component-testing.md)), the absence of an explicit rule creates a material risk of **AI model drift**: each subsequent agent session may make a different choice, producing an inconsistent codebase.

### Options Considered

| Option | Description | Assessment |
|--------|-------------|------------|
| **Domain entities all the way up** | Application service interfaces return domain entities; Razor components bind directly to `Label`, `Repository`, etc. | Simple, no mapping code. But couples presentation layer directly to domain model. Any domain change ripples into Razor components. |
| **DTOs at the Application→App boundary** | Repositories and internal Application services operate with domain entities. Public-facing Application service interfaces (called by `App`) return dedicated DTO/ViewModel records defined in the Application layer. | Explicit boundary. Domain model changes do not leak into UI. Aligns with both Uncle Bob's original article and the two dominant .NET reference implementations. |
| **Full CQRS with separate read/write models** | Separate query models (projections) and command models. Read side never touches domain entities. | Maximum flexibility. Overkill for a solo-developer tool with a small domain. Introduces significant boilerplate with no proportionate benefit at this scale. |

---

## Decision

**Apply a two-tier data-shape rule:**

1. **Repository boundary (Infrastructure ↔ Application):** `IRepository` interfaces return and accept **domain entity records** (`Label`, `Repository`, etc.). Infrastructure implementations translate external API responses (e.g. `LabelResponseDto` from the GitHub REST API) into domain records before crossing this boundary. The Application layer is permitted — and expected — to work directly with domain entities internally.

2. **Application→App boundary:** All public Application service interfaces (`ILabelManagerService`, `IRepositoryService`, etc.) — the interfaces called directly by Blazor components and pages — return and accept **dedicated DTO records** defined in the `SoloDevBoard.Application` layer. Domain entities must not appear in the return types or parameters of these interfaces.

DTOs are named `<Entity>Dto` (e.g. `LabelDto`, `RepositoryDto`) and are defined as `sealed record` types with `init`-only properties, co-located in the Application layer alongside the service interface that uses them.

### Boundary Map

```
[GitHub REST API]
        ↓  LabelResponseDto (private to Infrastructure)
[Infrastructure] — ToDomain() mapping
        ↓  Label (domain entity) — crosses ILabelRepository boundary
[Application — internal]
        ↓  LabelDto (Application DTO) — crosses ILabelManagerService boundary
[App — Blazor components]
```

---

## Rationale

### Strict Adherence to Uncle Bob's Original Intent

Robert C. Martin's 2012 "Clean Architecture" article explicitly states:

> *"We don't want to cheat and pass Entity objects across the boundaries… When we pass data across a boundary, it is always in the form that is most convenient for the inner circle."*

Passing a `Label` domain entity through `ILabelManagerService` to a Razor component is precisely the "cheat" Uncle Bob warned against. Domain entities are inner-circle objects; the App layer is outer-circle. A DTO defined in the Application layer is the correct "most convenient form for the inner circle" — it is shaped by the use-case, not by the domain model.

### Alignment with Dominant .NET Reference Implementations

Both leading .NET Clean Architecture reference projects confirm this as the established community pattern:

- **ardalis/CleanArchitecture (18k+ ★):** Handlers fetch domain entities from `IRepository<T>` (internal), then manually map to a DTO record (e.g. `ContributorDto`) at the handler boundary before returning outward. The DTO is defined in the `UseCases` layer — the Application equivalent.
- **jasontaylordev/CleanArchitecture (20k+ ★):** Uses AutoMapper `ProjectTo<TodoItemBriefDto>()` to project directly to a DTO; the domain entity never reaches the presentation layer. The DTO is defined in the Application layer.

Both projects position the DTO as the single type that crosses outward from Application. SoloDevBoard follows the ardalis pattern (manual mapping) since it wraps a REST API rather than an EF Core `IQueryable`.

### Future-Proofing the Domain Model

By returning DTOs from Application service interfaces, the domain model can evolve (adding properties, removing internal state, extracting value objects) without changing the interface contract visible to Razor components. A `Label` record gaining a new internal field does not require touching any component.

### AI Agent Consistency

SoloDevBoard is primarily developed by AI delivery agents that generate code in isolated sessions without full conversation context. An explicit ADR rule ensures every future agent session produces code consistent with this pattern without requiring the user to re-explain the decision.

---

## Consequences

### Immediate — Retrospective Updates Required

The following existing stubs violate this rule and must be updated before any consumer (Razor component) is built on top of them:

| File | Current (incorrect) | Required |
|------|---------------------|----------|
| `src/Application/SoloDevBoard.Application/Services/ILabelManagerService.cs` | Returns `IReadOnlyList<Label>` | Must return `IReadOnlyList<LabelDto>` |

A `LabelDto` record must be created in the Application layer as part of this retrospective fix.

### Ongoing Rules for All Future Work

- **Domain types are internal to Application.** They must not appear in the public signature of any `I*Service` or `I*Manager` interface.
- **DTOs live in Application.** A `<Entity>Dto` record is defined in `SoloDevBoard.Application`, co-located with the service interface that uses it.
- **Mapping happens in Application.** The Application service implementation maps from domain entity to DTO before returning. No mapping logic belongs in Razor components.
- **Repositories remain domain-typed.** `ILabelRepository`, `IRepositoryRepository`, etc., may freely use domain entities in their signatures — this is the correct inner-circle contract.
- **No AutoMapper.** Mapping is explicit and manual, consistent with the ardalis reference pattern and the project's preference for explicitness over convention-based magic.

### Relationship to ADR-0004

This ADR supplements [ADR-0004](0004-layered-architecture.md). ADR-0004 governs dependency direction (which projects reference which). ADR-0011 governs data-shape direction (which types cross which boundaries). Both rules must be satisfied simultaneously.
