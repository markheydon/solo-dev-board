---
name: Docs Update
description: Review and update repository documentation.
agent: Tech Writer
---

# Documentation Refresh

Use when documentation requires review or synchronisation.

Invoke the Tech Writer.

The Tech Writer is the authoritative source for documentation behaviour.

This prompt intentionally contains minimal logic.

---

## Typical Uses

Examples:

- Documentation Refresh
- Review user guides
- Review README and docs
- Refresh repository documentation
- Create ADR
- Perform documentation audit

---

## Expected Outcomes

- Documentation reviewed.
- Outdated information corrected.
- New documentation added where required.
- Links validated.
- UK English verified.

---

## Typical Scope

May include:

- README.md
- docs/
- docs/user-guide/
- infra/
- adr/

Planning artefacts should only be updated when explicitly requested.

---

## Example Output

Documentation Updated

Files Changed:
- README.md
- docs/index.md
- docs/user-guide/board-rules-visualiser.md

Summary:
- Updated feature status.
- Added user guide links.
- Corrected outdated references.

Validation:
- UK English verified.
- Links reviewed.
- Markdown validated.

---

## When Not To Use

Do not use this prompt for:

- Routine implementation work.
- Minor user-guide tweaks.
- Small documentation edits accompanying an issue implementation.
- Updating BACKLOG.md during normal delivery.

Those activities should normally be handled by the Delivery Agent.

---

## Next Steps

After documentation updates are complete:

- Continue implementation.
- Run Review Agent if implementation is complete.
- Create a release-related ADR if required.
