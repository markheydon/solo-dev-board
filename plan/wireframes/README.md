# Wireframe Reference Documents

This directory contains planning-only wireframe references for key SoloDevBoard pages. These artefacts are intended to guide implementation discussions, clarify user goals, and document layout and interaction patterns. No source code is included.

## How to Use
- Review each wireframe before proposing UI changes or new features.
- Use these documents to align on user goals, accessibility, and responsive behaviour.
- Reference the ASCII wireframes and interaction notes during implementation planning.

## Wireframes
- [repositories-wireframe.md](repositories-wireframe.md): Repositories page wireframe, command strip, and built-in Open source / Not open source catalogue filters (#440).
- [label-manager-wireframe.md](label-manager-wireframe.md): Label Manager page wireframe, mode separation rationale, tabbed IA, Labels-tab bulk delete (#444), and keep-`area/*` nested option (#446).
- [audit-dashboard-wireframe.md](audit-dashboard-wireframe.md): Audit Dashboard wireframe, KPI cards, health indicators, and filter surface.
- [one-click-migration-wireframe.md](one-click-migration-wireframe.md): One-Click Migration page wireframe, workflow-first layout, preview-first review flow, and post-migration summary states.
- [triage-ui-wireframe.md](triage-ui-wireframe.md): Triage UI wireframe, session flow, progress tracking, label/milestone/project-board actions, skip/return, and end-of-session summary.
- [board-rules-visualiser-wireframe.md](board-rules-visualiser-wireframe.md): Board Rules Visualiser wireframe, repository/project selection, interactive diagram, rule detail and conflict panels, compare mode, and responsive layout.
- [actions-templates-wireframe.md](actions-templates-wireframe.md): Actions Templates page wireframe, template browser, parameter editor, apply-to-repository flow, status/feedback region, and responsive layout.
- [auth-entry-wireframe.md](auth-entry-wireframe.md): Hosted sign-in landing page, PAT connectivity shell indicator, recovery pages, and manual test scenarios for issues #249 and #314.
- [planning-wireframe.md](planning-wireframe.md): Cross-Repo Planning hub, Daily Focus, Backlog Review, Iteration Planning (stall gate vs capacity meter, #445), and Repo Management for feature #272.
- [product-site-landing-wireframe.md](product-site-landing-wireframe.md): Public product site landing (`/`), navigation, feature grid, and release version badge.
- [product-site-about-wireframe.md](product-site-about-wireframe.md): Product About/history section (`/about/`), origin and AI-collaborator narrative pages.


## Wireframe-First Planning Pattern

For major UI refresh stories, create a wireframe artefact in this directory before implementation starts. Implementation stories should reference the approved wireframe and align on layout, interaction, accessibility, and responsive behaviour. Test issues should reference the same wireframe for coverage scope.

Recent examples:
- Repositories page refresh (#131) and tests (#132).
- Label Manager refresh (#133) and tests (#134).
- Audit Dashboard refresh (#135) and tests (#136).
- One-Click Migration page refresh (#139) and paired bUnit coverage (#140).
- Triage UI planning set (#142) and tests (#151–#153).
