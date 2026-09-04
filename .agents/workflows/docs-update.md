# Docs Update

**Contract:** [`.agents/contracts/tech-writer.md`](../contracts/tech-writer.md)
**Skills (on demand):** `documentation-writer`; `aspire` and `playwright-cli` when user-guide screenshots must be recaptured.

## Easy-to-miss specifics

- Documentation is the deliverable — use when docs themselves are the primary output.
- Do not use for minor documentation tweaks during normal implementation; Delivery handles those.
- When a published user-guide page or landing tile changes **materially in the UI**, recapture the matching `website/static/images/` screenshot in the same docs-update. Prefer `cd tests/E2E && npm run capture:docs` (or a focused `-g` filter) against a local app with a real PAT and `DocsCapture:Enabled=true`. See [plan/DOCS_STRATEGY.md](../../plan/DOCS_STRATEGY.md#screenshot-convention) and [tests/E2E/README.md](../../tests/E2E/README.md#documentation-screenshots).
- Do not treat screenshot recapture as optional follow-up. If PAT, Aspire, or docs-capture mode is missing, say so and stop that slice as blocked — do not mark the docs-update complete.
- Updating `tests/E2E/docs-capture/` helpers so the capture shows the documented loaded state is in scope. That is not application feature code.

## Invocation

**Chat:** "Refresh documentation for [topic]" or "Update the decision log for [decision]".
**Slash command:** `/docs-update`.
**GitHub Issue comment** (mention at the start; the issue supplies the topic unless named):
- `@cursor refresh the docs for this`
- `@cursor update the user guide for this`
- `@cursor update the decision log for [decision]`
