# Label Manager v1.1 dogfood — issue checklist

Parent: shipped Feature [#27](https://github.com/markheydon/solo-dev-board/issues/27) (closed). No new Feature issue (DEC-027 catch-up).

## Pre-creation

- [x] Scope validated (`plan/SCOPE.md` Label Manager).
- [x] Decision recorded (DEC-034).
- [x] Wireframe updated (`plan/wireframes/label-manager-wireframe.md`).
- [x] Stories #444 and #446 already exist on milestone `v1.1 - Cross-Repo Planning & Refinement`.

## Issues

- [x] Story [#446](https://github.com/markheydon/solo-dev-board/issues/446) — keep `area/*` out of built-in taxonomy cleanup (`type/story`, `size/s`).
- [x] Story [#444](https://github.com/markheydon/solo-dev-board/issues/444) — bulk delete on Labels tab (`type/story`, `size/m`).
- [x] Test [#457](https://github.com/markheydon/solo-dev-board/issues/457) — area catalogue and keep option (`type/test`, `size/s`, parent #446).
- [x] Test [#459](https://github.com/markheydon/solo-dev-board/issues/459) — Labels tab bulk delete (`type/test`, `size/s`, parent #444).
- [x] Sub-issues: #446 → #457; #444 → #459.
- [x] Blocking: #446 blocks #457; #444 blocks #459.
- [x] Project #8: Status Todo, Priority Medium, Phase blank, dates blank.

## Delivery sequence

1. #446, then its test issue.
2. #444, then its test issue.
3. [#445](https://github.com/markheydon/solo-dev-board/issues/445) is a separate Planning story (see `plan/iteration-planning-stall-capacity-project-plan.md`).
