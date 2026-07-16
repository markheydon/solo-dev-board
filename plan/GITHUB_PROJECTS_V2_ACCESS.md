# GitHub Projects v2 Access Limitations

This note records a known platform limitation affecting SoloDevBoard when using **hosted GitHub App sign-in**.

## Summary

GitHub can report that a repository has more linked Projects v2 boards than SoloDevBoard can load. In hosted mode, **private user-owned Projects v2** are commonly returned as inaccessible (`null` nodes with `Resource not accessible by integration`), while **public** linked projects continue to work.

This is not a SoloDevBoard parsing defect. It is a GitHub authentication and authorisation boundary for GitHub Apps.

## Runtime evidence

During debugging of `markheydon/mhcg-cs-mhcgintegrationapp`, GitHub GraphQL returned:

```json
"projectsV2": {
  "nodes": [null, { "title": "Mark's Workboard", ... }]
}
```

with a partial error of `Resource not accessible by integration` for the inaccessible node. The accessible project was public; the missing project was private and user-owned.

## Why GitHub App sign-in cannot read some linked projects

- Hosted sign-in uses a GitHub App **user-to-server** token (`ghu_…`).
- GitHub Apps are not supported as collaborators on private Projects v2 (`ProjectV2Actor` supports users and teams, not bots/apps).
- Repository **Projects: Read-only** and Organisation **Projects: Read-only** permissions do not reliably grant access to **private user-owned** Projects v2, even when the signed-in user is project admin.
- Public linked projects remain readable and are shown normally.

Reference discussion: [Unlocking GitHub Apps: Why Bots Need Access to Private Projects v2](https://devactivity.com/posts/apps-tools/unlocking-github-apps-why-bots-need-access-to-private-projects-v2-for-enhanced-productivity/).

## Product behaviour in SoloDevBoard

- SoloDevBoard counts linked project boards reported by GitHub, including inaccessible entries.
- Accessible supported boards (those with a Status field) are still shown.
- When `inaccessibleLinkedProjectCount > 0`, Board Rules and Triage show a warning explaining that some linked boards could not be loaded and suggesting PAT mode (`read:project`) or making the project public.

## Workarounds today

| Mode | Private user-owned Projects v2 |
|------|--------------------------------|
| Hosted GitHub App sign-in | Not supported by GitHub |
| PAT mode with `read:project` | Supported as the signed-in user |
| Make the project public | Supported under GitHub App sign-in |

## Future work

See `plan/BACKLOG.md` — **Private user-owned Projects v2 under hosted sign-in**. A durable fix likely requires either a GitHub platform change (Apps as Projects v2 collaborators) or a dedicated Projects v2 credential path (for example a PAT used only for Projects GraphQL calls).
