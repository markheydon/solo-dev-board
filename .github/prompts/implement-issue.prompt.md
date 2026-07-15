---
name: Implement Issue
description: Implement a planned GitHub issue.
agent: Delivery Agent
argument-hint: Specify issue number or feature name.
---

# Implement Issue

Use after planning is complete.

Invoke the Delivery Agent.

The Delivery Agent is the authoritative source for implementation behaviour.

This prompt intentionally contains minimal logic.

---

## Expected Outcomes

- Feature branch created or selected
- Implementation completed
- Tests added or updated
- Relevant documentation updated
- BACKLOG.md updated
- Ready for review

---

## Example Inputs

```text
Implement issue #184
```

```text
Build Label Manager UI
```

```text
Fix bug #42
```

```text
Implement issues #100 and #101
```

---

## Expected Output

```text
Implementation Complete

Issue: #184

Files Changed:
- BoardRules.razor
- BoardRules.razor.cs
- BoardRulesTests.cs

Tests:
- Added diagram rendering tests

Documentation:
- User guide updated

Backlog:
- Updated

Ready for Review Agent.
```

---

## Testing Phase

If the user begins testing the implementation:

- Remain on the same branch
- Apply fixes without committing
- Accumulate fixes during the session
- Create one summary commit when testing is complete

---

## Next Step

After implementation and testing are complete:

```text
Run Review Agent
```

to validate the work and create a pull request.
