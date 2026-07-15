---
name: Review and Close
description: Run lightweight review and create a pull request.
agent: Review Agent
argument-hint: Specify "review issue #X".
---

# Review and Close

Use after implementation is complete.

Invoke the Review Agent.

The Review Agent is the authoritative source for workflow behaviour.

Do not duplicate review rules in this prompt.

---

## Expected Outcomes

- Build validation
- Test validation
- Documentation validation (when applicable)
- Pull request creation

---

## Example Inputs

```text
Review issue #184
```

```text
Review feature/issue-184-board-rules-diagram
```

```text
Review Label Manager implementation
```

---

## Expected Output

```text
✅ Build passed
✅ Tests passed
✅ Documentation validated

PR #224 created.

Ready for merge.
```

---

## Notes

This prompt intentionally contains minimal logic.

All review behaviour, quality gates and PR workflow rules are defined in the Review Agent.

If review fails, the Review Agent will:

- Report the failure
- Explain the reason
- Hand work back to the Delivery Agent if code or documentation changes are required

If review succeeds, the Review Agent will:

- Create the pull request
- Apply appropriate metadata
- Notify the user that the work is ready for merge
