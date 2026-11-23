# Contract: collection service

Web calls this Application surface only. It does not inject `IGitHubService` or the store.

## `StartAsync(cancellationToken) -> StartResult`

1. Read `ICurrentUserContext.OwnerLogin` and `GetAccessToken()` on the request scope.
2. If the sanitised login already has a live task, return `AlreadyInProgress` and do not start another walk.
3. Otherwise persist `inProgress` (keep existing `repositories` and `lastSuccessfulCollectionAt`; if there was no document, `repositories` is empty and last success is null), remember the token only in the coordinator, and start the task.
4. Return `Started` without waiting for GitHub.

The task:

- Uses a new scope whose `ICurrentUserContext` is the captured login and token.
- Calls `IGitHubService.GetRepositoriesAsync(forceReload: true, cancellationToken: applicationStopping)`.
- On success, saves `succeeded`, sets `lastSuccessfulCollectionAt`, and replaces `repositories` from the catalogue fields (`GitHubId`, `OwnerLogin`, `Name`, visibility from `IsPrivate`, `IsArchived`, `IsFork`, `Url`). Duplicate `githubId` values collapse to one entry.
- On the mapped failures in [research.md](../research.md), saves `failed` and the reason, and does not replace `repositories` or `lastSuccessfulCollectionAt`.
- On host shutdown, attempts one `interrupted` / `siteStopped` save. If that save does not finish, the next `GetStatusAsync` still records interrupted.
- Removes the live task in `finally` and drops the token.

`cancellationToken` on `StartAsync` is the request token. It must not cancel the walk.

## `GetStatusAsync(cancellationToken) -> OperatorSnapshotStatusDto`

Read-only apart from the store's interrupted transition.

| DTO state | Meaning |
| --- | --- |
| `NeverRun` | No document. |
| `InProgress` | Live task, or stored in-progress that this process still owns. Includes `startedAt` and `repositoriesRecorded` (memory count when live). |
| `Succeeded` | `lastSuccessfulCollectionAt` and snapshot count. |
| `Failed` | `finishedAt`, short reason, and last success fields when a snapshot exists. |
| `Interrupted` | `finishedAt`, site-stopped reason, and last success fields when a snapshot exists. |

Also returns `DurabilityKind`: `LocalMachine`, `EphemeralContainerDisk`, or `HostedBlob`, chosen as in [research.md](../research.md).

The DTO has no repository list, no token, and no log.

## Catalogue source

The walk uses the existing user-repository list only. It does not call the owner-specific list, does not scrape `/repositories`, and does not add an `HttpClient`. Docs-capture filtering inside `GetRepositoriesAsync` still applies, so the snapshot matches repositories the product can already show.
