---
name: create-architectural-decision-record
description: >-
  Write a new Architectural Decision Record (ADR) under adr/ with the next
  adr-NNNN-[slug].md filename. Use when documenting a significant architecture or
  technology choice, superseding a prior decision, or when the user asks for an ADR.
---

# Create architectural decision record

## Instructions

1. Confirm the decision title, context, chosen option, alternatives, and stakeholders from the user or conversation. Ask if anything required is missing.
2. Determine the next sequence number by listing existing `adr/adr-*.md` files (four-digit `NNNN`).
3. Draft the ADR using [references/adr-document-template.md](references/adr-document-template.md). Use precise UK English; include positive and negative consequences and rejection rationale for alternatives.
4. Save only under repository-root `adr/` as `adr-NNNN-[title-slug].md`. Do not place ADRs under `docs/`.
5. Use coded bullets (3-4 letter prefix + three digits) in multi-item sections when using the full template.

## Requirements

- Machine-parseable structure and readable prose.
- Status appropriate to the request (`Proposed` until accepted).
- Do not rewrite accepted ADRs without documenting status change in the ADR body.

## Resources

| Path | Purpose |
| --- | --- |
| [references/adr-document-template.md](references/adr-document-template.md) | Full ADR markdown template |
| [adr/README.md](../../../adr/README.md) | Repository ADR folder conventions |
