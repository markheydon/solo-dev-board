---
name: Docs Update
description: This prompt automates the review and update of all repository documentation outside the `plan/` folder. So end-user docs, guides, and repo-level documentation are always in sync with the current feature set and project state. Use this prompt whenever you need to refresh documentation after new features are delivered, or when the project evolves.
agent: Tech Writer
---

## Purpose

This prompt automates the review and update of all repository documentation **outside the plan/** folder, ensuring that end-user docs, guides, and repo-level documentation are always in sync with the current feature set and project state. Use this prompt whenever you need to refresh documentation after new features are delivered, or when the project evolves.

---

## Trigger Phrase

- **Docs-Update**

---

## Scope

- **Includes:**
  - All `.md` files in `docs/`, `docs/user-guide/`, `README.md`, `infra/README.md`, `adr/README.md`, and similar repo-level docs.
- **Excludes:**
  - All files under `plan/` (managed by PM Orchestrator and Tech Writer processes).

---

## Workflow

1. **Review all documentation files outside plan/**:
   - List all `.md` files in `docs/`, `docs/user-guide/`, `README.md`, `infra/README.md`, `adr/README.md`.
   - Exclude any files in `plan/`.

2. **Determine current feature set and project state:**
   - Refer to `plan/BACKLOG.md`, `plan/SCOPE.md`, and other planning artefacts as needed to establish what features are available, partially available, or planned.

3. **Check each documentation file for accuracy:**
   - Ensure feature tables, status indicators, and descriptions match the current state.
   - Update quick links and user guide indexes to include new or changed features.
   - Expand or revise user guide pages for features that have changed or been delivered.
   - Enforce UK English spelling and bullet punctuation rules throughout.
   - Ensure architectural vocabulary is consistent with ADRs.

4. **Update documentation as needed:**
   - Edit files to synchronise feature status, descriptions, and cross-references.
   - Add or update links to new user guides or feature pages.
   - Remove outdated or misleading information.

5. **Validate style and cross-references:**
   - Confirm UK English and bullet rules are enforced.
   - Check that all links are valid and targets exist.
   - Ensure cross-references to ADRs, planning files, and code elements use correct format.

6. **Report changes:**
   - List files updated.
   - Summarise changes made.
   - Confirm style validation and cross-reference correctness.

---

## Example Invocation

**Prompt:**

```
Docs-Update
```

**Expected Outcome:**
- All repository documentation outside plan/ is reviewed and updated to reflect the current feature set and project state.
- Style and cross-reference validation is enforced.
- A summary of files updated and changes made is provided.

---

## Notes

- It is acceptable to refer to plan/ artefacts to determine the current state, but do not edit plan/ files as part of this process.
- This prompt is intended for end-user documentation and repo-level docs only.
- For planning artefacts, use the PM Orchestrator and Tech Writer processes.

---

## Style Requirements

- UK English spelling throughout.
- All bullet point list items that form complete sentences must end with a full stop (`.`).
- Architectural vocabulary consistent with ADRs and planning docs.
- Valid cross-references and links.

---

## Integration

- Use this prompt after feature delivery, before release, or when documentation needs a refresh.
- Works alongside PM Orchestrator and Tech Writer agents.

---

# End of Prompt
