# Contract: operator snapshot document

Normative JSON for one operator. Field rules and transitions are in [data-model.md](../data-model.md). This file is the shape writers and readers must round-trip.

Media type: `application/json`. Property names are camel case. Unknown properties at the document root may be ignored. Properties inside `laterFacts` must be preserved.

## Example: successful snapshot

```json
{
  "schemaVersion": 1,
  "operatorLogin": "octocat",
  "revision": 3,
  "lastSuccessfulCollectionAt": "2026-11-23T12:00:00Z",
  "run": {
    "status": "succeeded",
    "startedAt": "2026-11-23T11:59:00Z",
    "finishedAt": "2026-11-23T12:00:00Z",
    "repositoriesRecorded": 2,
    "reason": null,
    "detail": null
  },
  "repositories": [
    {
      "githubId": 1296269,
      "owner": "octocat",
      "name": "Hello-World",
      "visibility": "public",
      "archived": false,
      "fork": false,
      "htmlUrl": "https://github.com/octocat/Hello-World"
    },
    {
      "githubId": 1296270,
      "owner": "other",
      "name": "Forked",
      "visibility": "private",
      "archived": true,
      "fork": true,
      "htmlUrl": "https://github.com/other/Forked"
    }
  ],
  "laterFacts": {}
}
```

## Example: failed run that keeps the last success

`repositories` and `lastSuccessfulCollectionAt` stay as in the last success. `run` becomes:

```json
{
  "status": "failed",
  "startedAt": "2026-11-23T13:00:00Z",
  "finishedAt": "2026-11-23T13:01:00Z",
  "repositoriesRecorded": 1,
  "reason": "rateLimit",
  "detail": null
}
```

`reason` is `signInOrToken`, `rateLimit`, or `unexpected`. For `unexpected` only, `detail` may be a single line with the exception type and status code.

## Example: interrupted

```json
{
  "status": "interrupted",
  "startedAt": "2026-11-23T13:00:00Z",
  "finishedAt": "2026-11-23T13:05:00Z",
  "repositoriesRecorded": 0,
  "reason": "siteStopped",
  "detail": null
}
```

## Never run

No document. Callers must not invent a finish time or a snapshot count.
