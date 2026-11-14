# Reload from GitHub, keep selection — issue checklist

Parent epic: [#447](https://github.com/markheydon/solo-dev-board/issues/447). Milestone `v1.2 - Planning polish, Reload & Templates`.

## Pre-creation

- [x] Scope validated (`plan/SCOPE.md` Board Rules Visualiser `v1.2` reload slice).
- [x] No new DEC (extend `GitHubResponseCache` invalidation and Planning `forceReload`).
- [x] Wireframe updated (`plan/wireframes/board-rules-visualiser-wireframe.md`).
- [x] Epic #447, story #449, story #451 (relabelled from Feature), and ice-box story #450 already existed.

## Issues

- [x] Epic [#447](https://github.com/markheydon/solo-dev-board/issues/447) — body, size `l`, Implementation References.
- [x] Enabler [#485](https://github.com/markheydon/solo-dev-board/issues/485) — repository catalogue force-reload (`type/enabler`, `size/s`).
- [x] Story [#449](https://github.com/markheydon/solo-dev-board/issues/449) — Board Rules keep selection (`type/story`, `size/m`).
- [x] Test [#486](https://github.com/markheydon/solo-dev-board/issues/486) — Board Rules Reload (`type/test`, `size/s`, parent #449).
- [x] Story [#451](https://github.com/markheydon/solo-dev-board/issues/451) — remaining surfaces (`type/story`, `size/l`). Not a Feature: it is one delivery unit, and its only child is test #487.
- [x] Test [#487](https://github.com/markheydon/solo-dev-board/issues/487) — remaining surfaces (`type/test`, `size/m`, parent #451).
- [x] Story [#450](https://github.com/markheydon/solo-dev-board/issues/450) — window-focus refetch left ice-box, unmilestoned, `size/m`.

## Relationships

- [x] Sub-issue: #447 → #485, #449, #451, #450 (existing children plus enabler).
- [x] Sub-issue: #449 → #486.
- [x] Sub-issue: #451 → #487.
- [x] Blocking: #485 blocks #449.
- [x] Blocking: #449 blocks #486 and #451.
- [x] Blocking: #451 blocks #487.
- [x] Blocking: #449 and #451 block #450 (later follow-up).

## Project #8

- [x] New issues added. Status Todo, Priority from labels, Phase blank, dates blank.
- [x] Do not place this batch in Up Next (planning only).
