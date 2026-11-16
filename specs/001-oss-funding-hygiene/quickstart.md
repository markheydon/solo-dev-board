# Quickstart: validate OSS funding hygiene

End-to-end checks after implementation (`tasks.md`). Assumes local **Path A** PAT mode unless noted.

**Prerequisites**

- .NET 10 SDK, Aspire CLI 13.6.x aligned with AppHost SDK  
- GitHub PAT with access to repos tagged `open-source`  
- Docker (SQL container for `inventory`)

---

## 1. Start the distributed app

```bash
aspire start --non-interactive --isolated --apphost src/SoloDevBoard.AppHost/SoloDevBoard.AppHost.csproj
```

**Expect**

- `inventory` SQL resource becomes healthy  
- `inventory-migrations` reaches **Finished** (local `RunDatabaseUpdateOnStart`)  
- `app` and `functions` start after migrations  
- Dashboard shows no `FailedToStart` on migration resource

---

## 2. Confirm schema without using the UI

Inspect migration logs:

```bash
aspire logs inventory-migrations
```

**Expect**: `dotnet ef database update` (or bundle equivalent) applied with no errors.

---

## 3. Seed canonical standard and open-source repos

1. Sign in to the web app.  
2. Ensure at least one linked repo has topic `open-source`.  
3. Open **Open Source** from the main nav (route from wireframe).  
4. Save a non-empty canonical `FUNDING.yml` text when prompted.

**Expect**: Page does not list funding statuses until a run exists; empty state explains no inventory yet (FR-003 scenario 3).

---

## 4. Manual scan now

1. Click **Scan now** (primary toolbar action).  
2. Observe snackbar acknowledgement within a few seconds (SC-004).  
3. Watch Functions logs:

```bash
aspire logs functions
```

**Expect**: Queue message processed; run transitions to completed or partial; page shows new `last finished` time after refresh.

Repeat scan while a run is active.

**Expect**: Second request does not start duplicate walk; user sees already underway (FR-005).

---

## 5. Funding statuses

Use repos with: no file, exact standard match, one-line drift, and line-ending-only difference.

**Expect**

- Missing / matches / differs per FR-003 and SC-005  
- Line-ending-only match counts as matches  
- Archived repos visible but excluded from needs-attention count (FR-008)

---

## 6. Scheduled run (config change smoke test)

1. Stop Aspire.  
2. Set `Inventory__ScheduledCron` to a near-future minute (six-field CRON, UTC) via dashboard or `local.settings.json` for Functions.  
3. Restart and wait.

**Expect**: Timer fires without a signed-in browser (FR-006). Restore daily CRON after test.

---

## 7. Hosted sign-in path (staging)

Deploy or run staging with `hosted-sign-in-enabled`, valid `gh-app-id`, `gh-app-private-key`, and installation covering test repos.

**Expect**

- Overnight run completes with `AccessKind = Installation` on the run summary  
- Repo outside installation shows **not visible**, not missing (FR-016, SC-008)

---

## 8. Community Standards facts (stored, not on the page)

After a completed run, inspect snapshot rows for one non-fork repository (SQL or a test double of the scanner). The Open Source page still shows funding only.

**Expect**

- Description and README are single flags. A non-empty description and a README in that repository set them true.
- A health file that exists only in the owner `.github` repository sets the organisation-only flag true and the in-repository flag false (code of conduct, contributing, licence, security policy, issue templates, or pull request template).
- The same file inside the project repository sets the in-repository flag true and the organisation-only flag false.
- `LicenceName` is the repository licence when the repository has one.
- A fork is still checked. Its checklist is filled without the community profile call, which GitHub does not serve for forks.

## 9. Regression gates (agent)

```bash
dotnet format SoloDevBoard.slnx --verify-no-changes
dotnet build SoloDevBoard.slnx -c Release -warnaserror
dotnet test SoloDevBoard.UnitTests.slnf -c Release --no-build
```

Playwright: add journey for Open Source funding page per `USER_DOCS_ALIGNMENT.md` when UI ships.