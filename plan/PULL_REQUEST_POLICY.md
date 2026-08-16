# SoloDevBoard — Pull request policy

<!-- AI Collaborator Instructions: This is the canonical PR creation policy. Follow it whenever you open or update a pull request. Do not invent a different title style, body layout, or label set. -->

This document is the **single source of truth** for how pull requests are titled, labelled, linked, and described. Human contributors, GitHub Copilot, Cursor Cloud agents, Devin, and other automations all follow it.

Related artefacts:

- Template: [`.github/pull_request_template.md`](../.github/pull_request_template.md).
- Labels: [`LABEL_STRATEGY.md`](LABEL_STRATEGY.md).
- Verify workflow: [`.agents/workflows/verify-and-create-pr.md`](../.agents/workflows/verify-and-create-pr.md) and [`.agents/contracts/verify.md`](../.agents/contracts/verify.md).
- Branch naming for planned work: [`.agents/contracts/delivery.md`](../.agents/contracts/delivery.md) (Feature Branch).
- Roadmap board: [`PROJECT_BOARD_DESIGN.md`](PROJECT_BOARD_DESIGN.md) — do not add PR cards to Project #8.
- Decision: [DEC-022](DECISIONS.md#dec-022-canonical-pull-request-policy).

---

## Why this exists

PR metadata was previously split across the GitHub template, `CONTRIBUTING.md`, the Verify contract, the label strategy, Dependabot config, and vendor agent defaults. Those sources disagreed (title format, draft vs ready, template vs custom body). The result was inconsistent PRs, especially from AI agents. This file wins when other instructions conflict, including vendor defaults such as Cursor Cloud draft PRs or Conventional Commits titles.

---

## Title

Use this shape:

```text
[<Type>] <Imperative summary> (#<issue>)
```

Rules:

- `<Type>` is exactly one of: `Story`, `Feature`, `Enabler`, `Bug`, `Chore`, `Test`, `Documentation`.
- Match the linked issue `type/` label (`type/story` → `Story`, `type/documentation` → `Documentation`).
- Use sentence case after the type token, UK English, and an imperative verb (`Add`, `Fix`, `Document`, not `Added` or `Adding`).
- Append `(#N)` when the PR implements one issue. For several issues use `(#249, #314)`.
- Omit the issue suffix only for genuine ad-hoc work with no tracking issue.

Examples:

```text
[Story] Select a repository and project board to visualise (#183)
[Enabler] Persist hosted auth secrets via Key Vault at deploy time (#250)
[Bug] Fix Application Insights blocking local Aspire runs (#107)
[Chore] Automated application version stamping with MinVer (#344)
[Test] Add Playwright coverage for critical user journeys (#255)
[Documentation] Complete comprehensive user docs with safe public screenshots (#256)
```

Do **not** use:

- Conventional Commits prefixes (`feat:`, `fix:`, `chore(deps):`, `feat(#349):`).
- Label names in the title (`[type/story]`, `[type/chore]`).
- Unstable casing (`[story]`, `[WIP]` on a PR that is ready for review).
- The raw branch name as the title.
- Vague titles (`Update files`, `Implement issue #184` with no summary).

Dependabot may keep its generated `Bump …` titles. Prefer a `[dependabot]:` prefix when a workflow already applies one; do not rewrite Dependabot titles by hand unless they are misleading.

---

## Body

Always populate [`.github/pull_request_template.md`](../.github/pull_request_template.md). Required headings:

1. `## Summary of Changes`
2. `## Related Issue(s)`
3. `## Type of Change`
4. `## Checklist`
5. `## Screenshots (if applicable)`
6. `## Additional Notes`

Rules:

- UK English in all prose.
- Tick every applicable checklist item; leave non-applicable items unticked rather than deleting them.
- Put walkthroughs, test evidence, and agent-specific notes under **Additional Notes**, not as a replacement for the template.
- Do not submit an empty body, a commit-list-only body, or a vendor “Walkthrough” body that omits the template headings.

### How to create the body (tooling)

| Tool | What to do |
|------|------------|
| GitHub CLI | `gh pr create --fill --base main --head <branch>` so GitHub applies the template, then `gh pr edit` to fill headings, labels, assignee, and milestone. Do not pass a custom `--body` that drops the template. |
| GitHub web UI | Leave the pre-filled template in place and complete it. |
| Cursor Cloud `ManagePullRequest` / similar APIs that require a custom body | Copy the template headings verbatim into `body`. After create, set `draft` to `false` when the work is ready for review. |
| Copilot / other agents | Same content rules. Prefer the Verify workflow over ad-hoc PR creation. |

---

## Issue linking

- Planned work **must** link the tracking issue in **Related Issue(s)**.
- Use `Closes #N` (or `Closes #N, #M`) when merging the PR should close those issues.
- Use `References #N` when the PR is related but must not auto-close the issue (docs-only follow-up, partial delivery, investigation).
- Do not invent a tracking issue number. If none exists for planned feature work, stop and follow the planning workflow rather than opening an unlinked feature PR.

---

## Labels

Apply taxonomy labels from [`LABEL_STRATEGY.md`](LABEL_STRATEGY.md) on the **pull request** at creation time (not only on the issue).

| Group | On PRs | Rule |
|-------|--------|------|
| `type/` | Required | Copy from the linked issue, or choose the best match for ad-hoc work. |
| `priority/` | Required | Copy from the linked issue. Default `priority/medium` if the issue has none. Dependabot: `priority/low`. |
| `status/` | Required | `status/in-review` while the PR is open. Do not set `status/done` on an open PR. |
| `area/` | Required when the area is known | Copy from the issue. Use `area/docs` for documentation-only PRs and `area/infrastructure` for CI, Aspire, and deploy work. |
| `size/` | Optional | Copy from the issue when present. Do not invent a size on the PR alone. |

Also allowed: GitHub’s `dependencies` label on Dependabot PRs, in addition to the taxonomy labels.

Do **not**:

- Leave a human or agent PR with only `status/done` or with no `type/` / `priority/`.
- Put the pull request on Project #8 as a standalone card. Link it through `Closes #N` so it appears on the issue’s **Linked pull requests** field.

When the PR is opened, set the **issue** to `status/in-review` (remove `status/in-progress` / `status/todo`) as described in the Verify contract.

---

## Other metadata

| Field | Policy |
|-------|--------|
| Base branch | `main` unless the maintainer asked for another base. |
| Draft | **Ready for review** (`draft: false`) when Verify gates pass. Use draft only when the branch is known to be incomplete or blocked. Vendor defaults that create drafts must be overridden once the work is ready. |
| Assignee | `markheydon`. |
| Reviewers | Do **not** assign Copilot (or equivalent bot) as reviewer or assignee. |
| Milestone | Copy from the linked issue when present. |
| Projects | Do not add the PR to the SoloDevBoard Roadmap (Project #8). |

---

## Branches

Preferred for planned issues:

```text
feature/issue-N-short-kebab-description
```

Also accepted when a platform imposes the name:

- Cursor Cloud: `cursor/<descriptive-name>-<id>` (do not fight the platform suffix).
- GitHub Copilot coding agent: `copilot/…`.
- Dependabot: `dependabot/…`.

Never implement on `main`. Do not retitle the PR to match a vendor branch name.

---

## Dependabot

Dependabot PRs follow [`PROJECT_MANAGEMENT.md`](PROJECT_MANAGEMENT.md) and this label set:

- `type/chore`
- `priority/low`
- `status/in-review` (while open)
- `area/infrastructure`
- `dependencies` (config-generated)

They do not need a tracking issue or the `[Chore]` title form. Merge only after CI passes and a relevance review.

Configure those taxonomy labels in [`.github/dependabot.yml`](../.github/dependabot.yml) so new bot PRs are not created with `dependencies` alone.

---

## Agent checklist (copy this)

Before opening a PR:

1. Confirm the branch is not `main`.
2. Title matches `[<Type>] <Imperative summary> (#N)`.
3. Body includes every template heading, completed in UK English.
4. `Closes #N` or `References #N` is present when an issue exists.
5. Labels include `type/*`, `priority/*`, `status/in-review`, and `area/*` when known.
6. Assignee is `markheydon`; milestone is copied; Copilot is not assigned.
7. The PR is not draft unless the work is incomplete.
8. The PR is not added as a Project #8 card.

---

## Reviewers

When reviewing a PR for conventions, flag deviations from this file as process issues (title, template, labels, draft, missing `Closes #`). Coding review remains [`.agents/contracts/code-review.md`](../.agents/contracts/code-review.md).
