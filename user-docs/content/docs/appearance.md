---
weight: 20
title: Appearance
---

# Appearance

SoloDevBoard supports three theme modes so you can match your operating system preference or choose a fixed light or dark appearance.

## Theme Modes

| Mode | Behaviour |
|------|-----------|
| **Automatic** (default) | Follows your operating system's light or dark colour scheme. |
| **Light** | Always uses the light theme. |
| **Dark** | Always uses the dark theme. |

## Changing the Theme

1. Open any page that uses the main application shell (for example, Home or Repositories).
2. Click the **theme** button in the app bar (the icon changes to reflect the current mode).
3. Each click cycles through **Automatic â†’ Light â†’ Dark â†’ Automatic**.

The button's accessible name describes the current mode and the mode that will be activated next (for example, `Theme: light. Activate dark mode.`).

## Persistence

Your theme choice is saved in the browser's `localStorage` under the key `solo-dev-board.theme-preference`. The preference is restored on your next visit to SoloDevBoard in the same browser.

If browser storage is unavailable or cleared, SoloDevBoard defaults to **Automatic** mode.

## Notes

- The hosted sign-in landing page uses the same stored preference but does not include a theme button in the app bar.
- A small head script applies the correct page background before Blazor connects, reducing visible theme flash on reload.
