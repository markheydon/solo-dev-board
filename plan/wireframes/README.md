# Wireframe Reference Documents

This directory contains planning-only wireframe references for key SoloDevBoard pages. These artefacts are intended to guide implementation discussions, clarify user goals, and document layout and interaction patterns. No source code is included.

## How to Use
- Review each wireframe before proposing UI changes or new features.
- Use these documents to align on user goals, accessibility, and responsive behaviour.
- Reference the ASCII wireframes and interaction notes during implementation planning.

## Wireframes
- [repositories-wireframe.md](repositories-wireframe.md): Repositories page wireframe and interaction notes.
- [label-manager-wireframe.md](label-manager-wireframe.md): Label Manager page wireframe, mode separation rationale, and tabbed IA.
- [audit-dashboard-wireframe.md](audit-dashboard-wireframe.md): Audit Dashboard wireframe, KPI cards, health indicators, and filter surface.
- [one-click-migration-wireframe.md](one-click-migration-wireframe.md): One-Click Migration page wireframe, workflow-first layout, preview-first review flow, and post-migration summary states.

## Wireframe-First Planning Pattern

For major UI refresh stories, create a wireframe artefact in this directory before implementation starts. Implementation stories should reference the approved wireframe and align on layout, interaction, accessibility, and responsive behaviour. Test issues should reference the same wireframe for coverage scope.

Recent examples:
- Repositories page refresh (#131) and tests (#132).
- Label Manager refresh (#133) and tests (#134).
- Audit Dashboard refresh (#135) and tests (#136).
- One-Click Migration page refresh (#139) and paired bUnit coverage (#140).
