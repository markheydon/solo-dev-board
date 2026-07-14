# MudBlazor Inputs

Use this reference when selecting data entry and input components.

## Decision Guidance

- Use `MudTextField<T>` for most text capture scenarios.
- Use `MudNumericField<T>` for constrained numeric values.
- Use `MudSelect<T>` for known finite choices.
- Use `MudAutocomplete<T>` for large option sets and type-ahead.
- Use picker components (`MudDatePicker`, `MudTimePicker`, `MudColorPicker`) for specialised value types.
- Use `MudForm` when coordinating validation and submission across multiple fields.

## Component Coverage

| Component | When to use it. | Notes. |
|-----------|-----------------|--------|
| `MudAutocomplete<T>` | Type-ahead selection from larger collections. | Requires `MudPopoverProvider` in layout. |
| `MudCheckBox<T>` | Boolean input or checklist options. | Prefer over raw HTML checkboxes. |
| `MudColorPicker` | Colour selection with HEX, RGB, or palette style workflows. | Use `@bind-Text` for hex string models. |
| `MudDatePicker` | Single date capture. | Use for due dates, schedule dates, or report filters. |
| `MudField` | Shared field shell for custom input experiences. | Use when composing custom field content with consistent label and helper behaviour. |
| `MudFileUpload<T>` | File selection and upload workflows. | Keep validation explicit for file type and size constraints. |
| `MudForm` | Coordinated validation and submit flow across fields. | Use `Validate()` and form-level state instead of ad hoc per-control checks. |
| `MudFocusTrap` | Keep focus within a scoped region. | Useful for accessibility in dialogs and constrained overlays. |
| `MudHighlighter` | Highlight matched text fragments in search results. | Pair with user search text and sanitised matching logic. |
| `MudNumericField<T>` | Numeric values with min, max, and step. | Prefer this over text field parsing for numbers. |
| `MudRadioGroup<T>` + `MudRadio<T>` | Single selection from a short mutually exclusive set. | Better choice than select for small visible option groups. |
| `MudRating` | Rating input or display using stars or custom symbols. | Use for sentiment-style scoring, not critical binary decisions. |
| `MudSelect<T>` + `MudSelectItem<T>` | Dropdown for finite known options. | Supports multi-select when `MultiSelection="true"`. |
| `MudSlider<T>` | Range-based value selection. | Useful for bounded thresholds and tuning values. |
| `MudSwitch<T>` | Binary on/off state with immediate affordance. | Prefer where toggle semantics are clearer than checkbox semantics. |
| `MudTextField<T>` | Single-line and multi-line text input. | Supports adornments, masks, counters, and input type variants. |
| `MudTimePicker` | Time selection with 12h or 24h modes. | Bind with `@bind-Time` for `TimeSpan?` models. |
| `MudToggleGroup<T>` | Exclusive or multi-toggle selection via button-like options. | Useful for quick mode switches and compact option sets. |

## Related References

- For provider requirements and baseline setup, see `../SKILL.md`.
- For popover-related issues, see `KNOWN-PITFALLS.md`.
- For quick pattern matching, see `COMPONENT-CHOOSER.md`.
