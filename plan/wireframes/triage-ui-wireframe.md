# Triage UI Wireframe

## Purpose

The Triage UI provides a streamlined, keyboard-friendly interface for solo developers to triage unlabelled GitHub issues (and optionally unlabelled pull requests) one at a time. It is the core Phase 3 delivery work, designed to accelerate labelling, milestone assignment, project board management, and duplicate closure, while maintaining context and progress visibility.

**v1.2 update ([#492](https://github.com/markheydon/solo-dev-board/issues/492)):** The action surface uses mutually exclusive dispositions (Process, Duplicate, Skip) and a single primary commit per item instead of competing primary buttons for each write.

## User Goals

- Quickly triage unlabelled issues and pull requests with minimal friction.
- Apply labels, assign milestones, and add to project boards in one commit when processing an item.
- Close duplicates with reference to the original issue or pull request.
- Skip items and return later without losing context.
- Track progress and receive an end-of-session summary.
- Operate the UI entirely via keyboard, with clear focus and feedback.

## Layout

```
+---------------------------------------------------------------+
| Triage UI                                                     |
+---------------------------------------------------------------+
| Progress Bar      [Issue 7 of 42]                             |
|---------------------------------------------------------------|
| Issue/PR Details                                             |
|  - Title                                                     |
|  - Repository                                                |
|  - Author, Date                                              |
|  - Description (truncated, expandable)                        |
|---------------------------------------------------------------|
| Triage this item                                               |
|  Disposition: [ Process | Duplicate | Skip ]                 |
|  Process: Quick label, Milestone, Project board + status     |
|  Duplicate: Duplicate reference field only                   |
|  Skip: Optional skip reason only                             |
|  Primary: Save and next | Close as duplicate and next | Skip   |
|  Secondary: Skip item (Process/Duplicate) | Next without saving|
|---------------------------------------------------------------|
| Keyboard Shortcuts Legend                                    |
|---------------------------------------------------------------|
| End-of-Session Summary (shown after queue completion)         |
+---------------------------------------------------------------+
```

## Interaction Notes

- **Process (default):** Optional quick label, milestone, and project-board fields form one metadata surface. **Save and next** applies only filled values in order (label, milestone, project), records session summary entries for each write, then advances. Empty fields are no-ops. **Next without saving** advances without GitHub writes.
- **Duplicate:** Hides process metadata. Requires a duplicate reference. Primary action is **Close as duplicate and next**. Does not apply process metadata on this path.
- **Skip:** Session-only deferral with optional reason. Primary action is **Skip and next** (no GitHub writes). When Process or Duplicate is selected, **Skip item** remains a secondary action.
- Keyboard shortcuts: **Enter** or **L** commits the current disposition (when not typing in a field); **D** switches to Duplicate (or commits when Duplicate is already selected and the reference is valid); **S** skips.
- Focus is managed to ensure feedback is immediate after each commit.
- Aria-live regions provide immediate feedback for actions (e.g., "Label applied", "Milestone assigned").
- Skipped items are tracked and can be revisited within the session.
- Progress bar updates in real time as items are triaged.
- End-of-session summary displays counts of actions taken and skipped items.

Suggested test ids: `triage-disposition-toggle`, `triage-disposition-process`, `triage-disposition-duplicate`, `triage-disposition-skip`, `triage-save-and-next-button`, `triage-close-duplicate-and-next-button`, `triage-skip-and-next-button`, `triage-next-without-saving-button`, `triage-skip-item-button`.

## State Variants

- **Normal**: Issue/PR details and triage card visible, progress bar active.
- **Expanded Description**: Full issue/PR description shown on demand.
- **Process disposition**: Metadata form visible; primary is Save and next.
- **Duplicate disposition**: Reference field only; process metadata hidden.
- **Skip disposition**: Skip reason only; primary is Skip and next.
- **Session Complete**: Summary replaces triage interface, with option to review skipped items.

## Accessibility Notes

- All interactive elements are reachable and operable via keyboard (Tab, Shift+Tab, Enter, Space, and shortcuts).
- Disposition control exposes an accessible name and selected state.
- Focus order is logical and preserved when advancing through the queue.
- Aria-live feedback is used for action confirmations and error messages.
- Progress bar and summary are accessible to screen readers.
- No bespoke CSS unless MudBlazor primitives cannot satisfy accessibility requirements.

## Responsive Behaviour

- Layout adapts to smaller screens by stacking disposition controls and metadata fields vertically and collapsing the keyboard shortcuts legend.
- Progress bar remains visible at the top on all devices.
- Issue/PR details and the triage card are prioritised in mobile view, with expandable sections for less critical information.

---

This wireframe establishes the approved baseline for the Triage UI, including the v1.2 Save and next action model. All MudBlazor layout primitives and utility classes are to be used as the default approach, with bespoke CSS only as a last resort.
