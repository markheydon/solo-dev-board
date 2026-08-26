# MudBlazor Feedback and Overlays

Use this reference for user feedback, status communication, and transient surfaces.

## Component Coverage

| Component | When to use it. | Notes. |
|-----------|-----------------|--------|
| `MudAlert` | Inline contextual information, warnings, or errors. | Use for page-local messages that should remain visible. |
| `MudBadge` | Counts and concise status markers. | Pair with icons and tabs for lightweight indicators. |
| `MudDialog` via `IDialogService` | Modal flows requiring focused interaction. | Requires `MudDialogProvider` in layout. |
| `MudMessageBox` via service helpers | Standard confirmation and acknowledgement prompts. | Prefer for straightforward yes/no or ok/cancel prompts. |
| `MudProgressCircular` and `MudProgressLinear` | Loading and progress indication. | Choose circular for localised loading and linear for process progression. |
| `MudSkeleton` | Loading placeholders while content hydrates. | Improves perceived performance in content-heavy views. |
| `ISnackbar` with `MudSnackbarProvider` | Non-blocking global toasts. | Use for operation outcomes and short-lived notifications. |
| `MudOverlay` | Backdrop or blocking surface overlays. | Useful for busy states and controlled modal emphasis. |

## SoloDevBoard convention (DEC-035)

| Pattern | Use when | Examples |
|---------|----------|----------|
| **Snackbar** (`ISnackbar.Add`) | Brief outcomes after a user-initiated action; validation warnings before an action runs; operation success, partial success, or failure that does not need persistent visibility. | Label CRUD, migration apply complete, triage label applied, template apply summary, export copied to clipboard. |
| **Inline `MudAlert` / status region** | Page load or hydration errors that block content; empty states; setup guidance tied to visible controls; preview summaries the user must review before confirming; multi-item batch result panels. | Repository load failure with retry actions, migration requirements info, apply-result repository list, preview diff panels. |
| **Loading indicators** | In-flight work scoped to a control or section. | `MudProgressCircular` beside a selector, skeleton rows in a grid. |

### Rules

- Do not duplicate the same message in a snackbar and an inline alert.
- Do not use snackbars for in-progress operation feedback when an inline progress indicator or disabled control state already communicates the work in flight.
- Operation-complete feedback must not require scrolling on typical viewports (1400×900); prefer snackbars for transient outcomes on long pages.
- Map severity consistently: `Success` for completed work, `Warning` for partial success or recoverable issues, `Error` for failures (six-second snackbar duration), `Info` for neutral guidance.
- Global snackbar defaults live in `Program.cs` (bottom-right, five-second duration, outlined variant).
- Retain `aria-live="polite"` and `role="status"` on inline regions that surface persistent page state; snackbars inherit MudBlazor provider accessibility.

### Feature inventory (2026-08-25)

| Feature | Snackbar | Inline | Notes |
|---------|----------|--------|-------|
| Label Manager | CRUD, taxonomy/sync apply summaries, validation | Load errors with retry | Batch apply results stay inline in tab panels. |
| One-Click Migration | Apply/preview outcomes, API errors | Requirements info, preview warnings, result summaries | Bottom guidance region keeps contextual alerts only. |
| Triage | Session and triage action outcomes | Empty repositories, per-item warnings | Operation alert removed in favour of snackbars. |
| Actions Templates | Apply summaries and errors | Per-repository apply result list | Summary toast; detailed results stay inline. |
| Repositories | Placeholder actions | Load-state banner at page top | No duplicate snackbar for the same placeholder message. |
| Audit | Export actions | Load/error regions | Existing split retained. |
| Planning | Panel mutations | Load/error in panels | Existing snackbar usage retained. |

## Decision Guidance

- Use `MudAlert` when the message should stay anchored in page context.
- Use snackbar for brief outcomes that do not require immediate action.
- Use dialog or message box when explicit acknowledgement is required.
- Use skeletons for content loading states where layout stability matters.

## Related References

- For provider setup and service injection patterns, see `../SKILL.md`.
- For common provider and popup failures, see `KNOWN-PITFALLS.md`.
- Decision log: [DEC-035](../../../plan/DECISIONS.md#dec-035-transient-feedback-via-snackbar).
