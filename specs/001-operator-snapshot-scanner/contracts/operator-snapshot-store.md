# Contract: snapshot store port

Application port. Infrastructure supplies the file adapter and the blob adapter. Web chooses one. Lean runs with the file adapter when `BlobContainerClient` is not registered.

## Operations

`GetAsync(operatorLogin, cancellationToken)`

- Sanitise `operatorLogin`. If it is unsafe, do not touch the filesystem or the container. Return a failed result the collection maps to an unexpected error.
- Missing document means never run.
- If `run.status` is `inProgress` and the process coordinator reports no live task for that login, persist `interrupted` / `siteStopped` with a new `revision` and `finishedAt` of now (`TimeProvider`). Do not change `repositories` or `lastSuccessfulCollectionAt`.
- Return the document and its concurrency token (file: `revision`; blob: `revision` plus Azure `ETag`).

`SaveAsync(operatorLogin, document, expectedToken, cancellationToken)`

- Sanitise as above.
- File: under a lock for that path, re-read `revision`. If it differs from `expectedToken`, fail the save and leave the current file in place. Otherwise write `{name}.json.tmp` in the same directory and replace `{name}.json`.
- Blob: upload with `IfMatch` when an etag was read, or `IfNoneMatch = ETag.All` when creating. On conflict, fail the save and leave the existing blob in place.
- `document.revision` is the previous revision plus one.
- Do not log the document body (it lists private repositories).

Both adapters must pass the same contract tests with a fake file system and a fake blob:

- Two logins do not read or write each other's documents.
- A failed save leaves the previous successful `repositories` and `lastSuccessfulCollectionAt` intact (the caller writes a failed `run` onto a copy; the test asserts a rejected stale token does not clobber, and a successful failed-run write keeps the repository array).
- `../`, slashes, and other unsafe keys do not escape the snapshot directory or the blob container.
- No document versus a failed run with an empty `repositories` array remain distinct.
- An `inProgress` document with no live task becomes `interrupted` on read and a following save of a new `inProgress` run is allowed.

## Registration

- File adapter: always available. Path `{content root}/data/operator-snapshots`.
- Blob adapter: only when Web has called `AddAzureBlobContainerClient("operator-snapshots")` because Aspire injected that connection. Register the blob adapter **before** `AddSoloDevBoard`, and have Infrastructure `TryAdd` the file adapter so the blob registration wins.
- Application and Infrastructure do not call `GetConnectionString` and do not construct the client.

## Conditional write token

| Adapter | Token | Stale writer |
| --- | --- | --- |
| File | `revision` under the per-file lock, plus temp-file replace | Mismatch: no replace |
| Blob | Azure `ETag` via `BlobRequestConditions`, and `revision` in the body | HTTP 412: no replace |
