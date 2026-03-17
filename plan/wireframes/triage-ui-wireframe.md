# Triage UI Wireframe

## Purpose

The Triage UI provides a streamlined, keyboard-friendly interface for solo developers to triage unlabelled GitHub issues (and optionally unlabelled pull requests) one at a time. It is the core Phase 3 delivery work, designed to accelerate labelling, milestone assignment, project board management, and duplicate closure, while maintaining context and progress visibility.

## User Goals

- Quickly triage unlabelled issues and pull requests with minimal friction.
- Apply labels, assign milestones, and add to project boards efficiently.
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
| Actions                                                      |
|  [Apply Label] [Assign Milestone] [Add to Project Board]      |
|  [Close as Duplicate] [Skip/Return Later]                     |
|---------------------------------------------------------------|
| Keyboard Shortcuts Legend                                    |
|---------------------------------------------------------------|
| End-of-Session Summary (shown after queue completion)         |
+---------------------------------------------------------------+
```

## Interaction Notes

- All actions are accessible via keyboard shortcuts (e.g., `L` for label, `M` for milestone, `P` for project board, `D` for duplicate, `S` for skip).
- Focus is managed to ensure the next actionable element is always highlighted after each operation.
- Aria-live regions provide immediate feedback for actions (e.g., "Label applied", "Milestone assigned").
- Skipped items are tracked and can be revisited within the session.
- Progress bar updates in real time as items are triaged.
- End-of-session summary displays counts of actions taken and skipped items.

## State Variants

- **Normal**: Issue/PR details and actions visible, progress bar active.
- **Expanded Description**: Full issue/PR description shown on demand.
- **Duplicate Closure**: Reference input field appears for linking to original item.
- **Skip Mode**: Item marked for later review, UI advances to next in queue.
- **Session Complete**: Summary replaces triage interface, with option to review skipped items.

## Accessibility Notes

- All interactive elements are reachable and operable via keyboard (Tab, Shift+Tab, Enter, Space, and shortcuts).
- Focus order is logical and preserved when advancing through the queue.
- Aria-live feedback is used for action confirmations and error messages.
- Progress bar and summary are accessible to screen readers.
- No bespoke CSS unless MudBlazor primitives cannot satisfy accessibility requirements.

## Responsive Behaviour

- Layout adapts to smaller screens by stacking actions vertically and collapsing the keyboard shortcuts legend.
- Progress bar remains visible at the top on all devices.
- Issue/PR details and actions are prioritised in mobile view, with expandable sections for less critical information.

---

This wireframe establishes the approved baseline for the Triage UI, ensuring consistent planning and implementation for the remaining Phase 3 delivery work. All MudBlazor layout primitives and utility classes are to be used as the default approach, with bespoke CSS only as a last resort.
