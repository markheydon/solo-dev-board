---
name: Address PR Review Comments
description: Reviews coding review comments on an open pull request, implements the requested changes, replies on each addressed thread, resolves the conversations, and posts a final summary comment. Invokes Delivery Agent.
agent: Delivery Agent
---

# Address PR Review Comments Workflow

**When to use:** After a pull request is open and coding review comments request follow-up changes.

---

## Purpose

Execute the full PR review comment remediation workflow:
1. Fetch unresolved coding review comments on the pull request.
2. Implement the requested code changes on the existing PR branch.
3. Post a reply on each addressed coding review thread.
4. Resolve each addressed conversation.
5. Post one final summary comment on the pull request.

**Result:** The PR branch is updated, review threads are answered and resolved, and the pull request has a concise summary of the follow-up work.

---

## Inputs Required

Provide **ONE** of:
- **Pull request number:** "Address PR review comments on PR #86".
- **Explicit instruction:** "Review coding review comments on PR #86 and implement. Post comments and resolve the conversation on each coding review comment. Also, post a summary new comment on the PR once all done.".

---

## Workflow Steps

This prompt invokes the **Delivery Agent**, which executes:

### 1. Review Comment Discovery
- Fetch the pull request details and unresolved coding review comments.
- Identify which comments require code changes versus clarification only.
- Stay on the existing pull request branch; do not create a new branch.

### 2. Implementation
- Apply the requested code changes.
- Run the relevant validation steps for the affected files or tests.
- Keep the pull request open; do not create a replacement PR.

### 3. Thread Responses
- Post a reply on each addressed coding review comment thread describing what changed.
- Resolve each addressed conversation after the fix is in place.
- If a comment needs clarification or is intentionally not implemented, reply with the reasoning and leave the conversation unresolved.

### 4. Final PR Summary
- Post one new summary comment on the pull request after all addressed comments are handled.
- Summarise which comments were addressed, what changed, and any threads that remain open.

---

## Outputs Produced

### Artefacts Updated
- **Code on the existing PR branch.**
- **Review threads** — each addressed coding review comment has a reply and is resolved.
- **Pull request summary comment** — one final comment describing the completed follow-up work.

### Completion Summary Delivered
```markdown
# PR Review Comment Update Complete

- PR #86 updated with requested code changes.
- 4 coding review conversations replied to and resolved.
- Relevant tests rerun successfully.
- Final summary comment posted on the PR.
- 0 review conversations left open.
```

---

## Follow-Up Prompts

After PR review comments are addressed:
- **To continue the normal approval flow:** Return to `review-and-close.prompt.md` if another review pass is needed.
- **To finish the work after merge:** Run the close/merge workflow as normal.

---

## Example Invocation

**You say:**
```
Review coding review comments on PR 86 and implement. Post comments and resolve the conversation on each coding review comment. Also, post a summary new comment on the PR once all done.
```

**Agent responds by:**
- Implementing the requested changes on the existing PR branch.
- Replying on each addressed coding review comment.
- Resolving each addressed conversation.
- Posting a final summary comment on PR #86.