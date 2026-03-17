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

## Decision Guidance

- Use `MudAlert` when the message should stay anchored in page context.
- Use snackbar for brief outcomes that do not require immediate action.
- Use dialog or message box when explicit acknowledgement is required.
- Use skeletons for content loading states where layout stability matters.

## Related References

- For provider setup and service injection patterns, see `../SKILL.md`.
- For common provider and popup failures, see `KNOWN-PITFALLS.md`.
