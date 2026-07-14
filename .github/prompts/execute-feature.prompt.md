---
name: Execute Feature
description: Implements a planned GitHub issue following coding standards, creates tests, updates documentation, and ensures all mandatory gates are met. Invokes Delivery Agent.
agent: Delivery Agent
argument-hint: Specify issue number or feature name, e.g., 'implement issue #15' or 'build Label Manager UI'.
---

# Execute Feature (Copilot adapter)

Before executing, read and follow the canonical workflow in [`.agents/prompts/execute-feature.md`](../../.agents/prompts/execute-feature.md) in full.
Act as the assigned agent. Follow [`.agents/agents/delivery.md`](../../.agents/agents/delivery.md) for boundaries and handoffs.

Apply all project standards from [`AGENTS.md`](../../AGENTS.md).
