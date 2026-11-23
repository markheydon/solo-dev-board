# Data model: Operator snapshot document

One JSON document per operator. There is no relational schema and no application database. The file adapter and the blob adapter store this same document. Later features may add properties under `laterFacts`. This feature defines none of those properties (no community-standard fields, no FUNDING fields).

## Document

| Field | JSON | Type | Rules |
| --- | --- | --- | --- |
| Schema version | `schemaVersion` | number | `1` for this feature. A reader that does not understand the number does not treat the body as a snapshot and does not delete it. |
| Operator login | `operatorLogin` | string | The GitHub login from `ICurrentUserContext.OwnerLogin`. Never a display name. Must match the sanitised key used in the file or blob name. |
| Revision | `revision` | number | Positive integer. Increments on every successful write. Stale writers must not publish a document with an older revision. |
| Last success | `lastSuccessfulCollectionAt` | string (ISO-8601 UTC) or `null` | Set only when a walk succeeds. Unchanged when a later run fails or is interrupted. `null` means no successful snapshot is kept. |
| Run | `run` | object | Current run only. No history. |
| Repositories | `repositories` | array | Entries from the latest **successful** walk. Not replaced by a failed, interrupted, or in-progress walk. Empty array is valid after a successful walk of an empty catalogue. |
| Later facts | `laterFacts` | object | Always present. This feature writes `{}`. Round-trip must keep unknown properties inside this object. |

### `run`

| Field | JSON | Type | Rules |
| --- | --- | --- | --- |
| Status | `status` | string | `inProgress`, `succeeded`, `failed`, or `interrupted`. |
| Started | `startedAt` | string (ISO-8601 UTC) | When this run started. |
| Finished | `finishedAt` | string or `null` | When this run succeeded, failed, or was recorded as interrupted. `null` while `inProgress`. |
| Recorded | `repositoriesRecorded` | number | Count for **this run**. While the walk is live in this process, the status read model uses the in-memory count. The stored number is updated at the start (0) and on the terminal write. It is not a second catalogue. |
| Reason | `reason` | string or `null` | `null` when `succeeded` or `inProgress`. Otherwise one of `signInOrToken`, `rateLimit`, `siteStopped`, `unexpected`. |
| Detail | `detail` | string or `null` | Only with `unexpected`. One line: exception type and HTTP status code if any. No body, token, secret, or log. Otherwise `null`. |

`siteStopped` is stored when the host cancellation fires and the terminal write succeeds, and when a later read finds `inProgress` with no live task. The operator-facing sentence is fixed: the site stopped before the collection finished. `signInOrToken` is "a sign-in or token problem". `rateLimit` is "a GitHub rate limit". `unexpected` is "an unexpected error".

### Repository entry

| Field | JSON | Type | Rules |
| --- | --- | --- | --- |
| GitHub id | `githubId` | number | The list payload id as a JSON number (64-bit). Stable identity. One id once in `repositories`. |
| Owner | `owner` | string | `owner.login` from that same list payload. |
| Name | `name` | string | Repository name. |
| Visibility | `visibility` | string | `public` or `private`. |
| Archived | `archived` | boolean | Independent of `fork`. |
| Fork | `fork` | boolean | Independent of `archived`. Both may be `true`. |
| HTML URL | `htmlUrl` | string | GitHub `html_url`. |

Do not store description, topics, file contents, tokens, secrets, or a run log.

## Identity and names

Sanitised key: GitHub login grammar only. One to 39 characters, ASCII letters and digits, interior single hyphens, must not start or end with a hyphen (`^[A-Za-z0-9]([A-Za-z0-9-]{0,37}[A-Za-z0-9])?$` with the single-character form allowed). Reject empty, `..`, slashes, backslashes, and any other character.

- File name: `{sanitised}.json` inside `{content root}/data/operator-snapshots/`. After `Path.GetFullPath`, the path must still start with the full path of that directory. If it does not, do not open it.
- Blob name: `{sanitised}.json` in the fixed container `operator-snapshots`. No extra prefix taken from the login.

Unsafe keys do not get a document. The collection fails as an unexpected error rather than writing outside the directory or container.

Hosted isolation: the web action never supplies the key. Hosted sign-in uses the owner-login claim. Two signed-in operators therefore have two documents. PAT-only local trusted mode uses the PAT owner login the product already resolves.

## Absence versus run state

| Situation | Store | Page |
| --- | --- | --- |
| Never collected | No document | No snapshot is kept. No finish time and no snapshot count. |
| Success, including zero repositories | `run.status = succeeded`, `lastSuccessfulCollectionAt` set, `repositories` length is the count | Succeeded, finish time, count (zero is allowed). |
| Failed, nothing ever succeeded | Document exists, `lastSuccessfulCollectionAt` null, `repositories` empty, `run.status = failed`, one reason | No snapshot is kept. Failed time and short reason. |
| Failed or interrupted after a success | Previous `repositories` and `lastSuccessfulCollectionAt` unchanged; `run` is the new outcome | Last success time and count remain. Current run shows failed or interrupted and the short reason. |
| Live walk | `run.status = inProgress` and this process holds the task | In progress, start time, recorded count from memory. Start does not launch another walk. |
| Process died mid-walk | Stored `inProgress`, no live task | Next read stores `interrupted` / `siteStopped` and does not block a new collection. |

## State transitions

```text
(no document) --start--> inProgress --success--> succeeded
                      \--fail-----> failed
                      \--process death, next read--> interrupted

succeeded --start--> inProgress --success--> succeeded (repositories replaced)
                 \--fail or death--> failed or interrupted (repositories kept)

failed or interrupted --start--> inProgress
```

Signing out, leaving `/repositories`, or reloading the live catalogue does not change the document.

## Who writes what

| Type | Home | Notes |
| --- | --- | --- |
| `OperatorSnapshotDocument` and entry records | Application | JSON shape above. Web never sees the raw document. |
| `OperatorSnapshotStatusDto` | Application | Never run, or the run state, times, counts, short reason, last success, and durability kind. No repository list. |
| `IOperatorSnapshotStore` | Application port, Infrastructure adapters | Get and conditional save by operator login. |
| File adapter | Infrastructure | Temp file then replace. No blob types required. |
| Blob adapter | Infrastructure | Injected `BlobContainerClient` only. |
| Catalogue fields `GitHubId` (`long`), `OwnerLogin`, `IsFork` | Domain `Repository` | Filled from the existing list DTO. Existing `Id` (`int`) stays for current callers. |
