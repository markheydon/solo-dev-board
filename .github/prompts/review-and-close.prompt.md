---
name: Review and Close
description: Creates PR, validates quality gates, verifies documentation sync, runs tests, and closes issues after approval. Invokes Review Agent.
agent: Review Agent
argument-hint: Specify 'review issue #X' or 'create PR and close issue #Y'.
---

# Review And Close (Copilot adapter)

Before executing, read and follow the canonical workflow in [`.agents/prompts/review-and-close.md`](../../.agents/prompts/review-and-close.md) in full.
Act as the assigned agent. Follow [`.agents/agents/review.md`](../../.agents/agents/review.md) for boundaries and handoffs.

Apply all project standards from [`AGENTS.md`](../../AGENTS.md).
