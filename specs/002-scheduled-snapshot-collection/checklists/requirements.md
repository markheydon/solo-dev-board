# Specification Quality Checklist: Scheduled snapshot collection

**Purpose**: Validate specification completeness and quality before proceeding to planning
**Created**: 2026-11-23
**Feature**: [spec.md](../spec.md)

## Content Quality

- [x] No implementation details (languages, frameworks, APIs)
- [x] Focused on user value and business needs
- [x] Written for non-technical stakeholders
- [x] All mandatory sections completed

## Requirement Completeness

- [x] No [NEEDS CLARIFICATION] markers remain
- [x] Requirements are testable and unambiguous
- [x] Success criteria are measurable
- [x] Success criteria are technology-agnostic (no implementation details)
- [x] All acceptance scenarios are defined
- [x] Edge cases are identified
- [x] Scope is clearly bounded
- [x] Dependencies and assumptions identified

## Feature Readiness

- [x] All functional requirements have clear acceptance criteria
- [x] User scenarios cover primary flows
- [x] Feature meets measurable outcomes defined in Success Criteria
- [x] No implementation details leak into specification

## Notes

- Validation passed on 2026-11-24 (sixth iteration: once a day at 05:00 UTC is the normal cadence, not a cap; a missed or technically failed scan may be queued and run again the same UTC day; duplicate entries while already queued or in progress remain forbidden).
- Validation passed on 2026-11-24 (fifth iteration: queue fill at 05:00 UTC default separated from time-agnostic queue processing; FR-021 added).
- Validation passed on 2026-11-23 (fourth iteration, after the next-occasion and admission revision).
- The daily start is the same operator snapshot collection, with hand-started collection kept in both postures.
- Lean local posture, including a personal deployment that stays in that posture, has no schedule. The page explains that, and the operator can still collect when they ask.
- Fixed choices recorded as assumptions, not open questions: the fill set is people who completed hosted sign-in inside the last 30 days and who are currently allowed at fill time; an allow list is not scanned in full; losing permission stops queueing at the next fill; a sign-in or token failure pauses later scheduled fills until the next completed sign-in, which restarts the 30 days, and a later run still requires permission; the normal request is 05:00 UTC once each UTC day and is not a cap; a missed request or a technical failure may be queued again the same day; queue processing at any time of day; no second queue entry while already queued or in progress; no operator control for fill time, frequency, or the 30 days in this feature. The request phrase "5 p.m." is recorded as the existing 05:00 UTC plan, not 17:00.
- The Repositories page names the next 05:00 UTC only when that occasion will start a collection for that operator, including when none has ever run. A stored-access failure, a closed 30-day window, or a loss of permission is stated as no scheduled collection. The last successful finish time remains visible.
- A daily rate-limit or unexpected-error failure is tried again up to 3 times after a short delay, as one run. A sign-in or token failure is not retried and still pauses later days until the next completed sign-in. The final failure reason is shown with the last successful finish time when one exists. No new hand-started retry control is added.
