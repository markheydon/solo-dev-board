# Feature Specification: OSS funding file hygiene

**Feature Branch**: `001-oss-funding-hygiene`

**Created**: 2026-11-16

**Status**: Draft

**Input**: User description: "Review GitHub issue #439 (cross-repo FUNDING.yml presence and standard-file check). This is the first feature that depends on an overnight inventory of every repository linked to the operator and classified as open source. Specify what that inventory collects, how collection works when nobody has the product open (including hosted sign-in, where there is no active browser session), and a new Open Source page that later open-source hygiene features can share."

## Clarifications

### Session 2026-11-16

- Q: When the inventory records a Community Standards item as present, should a file that exists only in the organisation's shared `.github` repository count as present for that project repository? → A: Record both facts: present in the project repository, and present only via an organisation default.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - See funding hygiene without visiting each repository (Priority: P1)

A solo developer who keeps a preferred sponsorship file for open-source projects opens Solo Dev Board and sees, in one place, which of those projects are missing the file and which have a copy that is not the standard. The page reads the latest finished inventory. It does not contact GitHub for every repository while the page is opening.

**Why this priority**: This is the outcome in issue #439. Without a trustworthy snapshot view, the overnight inventory has nothing to show.

**Independent Test**: With a completed inventory already stored, open the Open Source page and confirm each in-scope repository shows one of: missing, matches the standard, or present but differs. Confirm the page shows when the inventory last finished, and that opening it does not start a fresh pass over the catalogue.

**Acceptance Scenarios**:

1. **Given** a finished inventory exists for the operator's open-source repositories, **When** the operator opens the Open Source page, **Then** each repository shows whether the funding file is missing, matches the configured standard, or is present but differs, together with the repository name and a way to open that repository on GitHub.
2. **Given** a repository has no funding file of its own, **When** the operator reads its row, **Then** the status is missing from that repository, and any note about an organisation-level default is separate from that status.
3. **Given** the inventory has never completed, **When** the operator opens the page, **Then** the page explains that no inventory exists yet and does not present an empty table as if every repository were healthy.
4. **Given** the latest inventory is older than the freshness window, **When** the operator opens the page, **Then** the page still shows the last results and states that they are stale, including the last finished time.
5. **Given** the latest inventory finished with some repositories unchecked, **When** the operator opens the page, **Then** successful rows stay visible and unchecked repositories are marked as not scanned, with a short reason, rather than shown as missing.

---

### User Story 2 - Keep one standard funding file (Priority: P1)

The operator keeps a single canonical funding file in Solo Dev Board. The inventory compares each repository's own file with that text. The operator can tell a real copy from a file that has drifted.

**Why this priority**: Presence alone does not meet the issue. The point is a preferred file, applied consistently. The comparison is only meaningful once the standard exists.

**Independent Test**: Save a canonical funding file, run or supply an inventory against repositories that contain that exact text, a different text, and no file, and confirm the three statuses. Change the standard and confirm the next inventory reclassifies rows. The previous snapshot does not change until that next inventory finishes.

**Acceptance Scenarios**:

1. **Given** no canonical file has been saved, **When** the operator opens the Open Source page, **Then** the page asks for the standard before it treats any repository as matching or differing, and it does not invent a standard from an arbitrary repository.
2. **Given** a canonical file is saved, **When** an inventory compares a repository file, **Then** "matches" means the repository text is the same as the standard after line-ending normalisation, and any other textual difference (including key order, comments, and platform entries) is "differs".
3. **Given** a repository has both `.github/FUNDING.yml` and a root `FUNDING.yml`, **When** the inventory records the repository, **Then** `.github/FUNDING.yml` is the file that is compared, and the root file is noted only as an extra copy.
4. **Given** the operator replaces the canonical file, **When** they return to the page before the next inventory finishes, **Then** the page warns that the visible statuses were compared with the previous standard.

---

### User Story 3 - Inventory open-source repositories while the product is closed (Priority: P1)

Once a day, without anyone using the site, Solo Dev Board inventories every linked repository that is classified as open source. Each checked repository stores funding-file facts and Community Standards checklist facts. The Open Source page shows the funding facts. The checklist facts stay stored for a later view. The operator can also ask for a run now. That request is accepted immediately. The page does not wait for the run to finish.

In personal-token operation the run uses the same long-lived operator credential the site already uses. In hosted sign-in the run uses the GitHub App installation credential, which the server can renew when nobody is signed in. A repository the installation cannot see is reported as not visible, with its funding status left blank.

**Why this priority**: This is the first feature that needs the inventory. Community Standards will read the checklist facts from this same pass. The run has to work when no browser session exists.

**Independent Test**: With the site closed (no signed-in session), a scheduled run completes and the next visit shows a new last-finished time and updated funding rows. For every repository the run checked, stored results also include the Community Standards checklist, and the page still shows only funding hygiene. A "scan now" control returns at once, and a second request while a run is already queued or in progress does not start a duplicate walk of the same scope. In hosted sign-in, a repository the installation cannot see is listed as not visible.

**Acceptance Scenarios**:

1. **Given** open-source repositories are linked to the operator, **When** the daily inventory runs and nobody has the product open, **Then** a finished run is stored covering that scope, and the next page visit shows those results.
2. **Given** the operator chooses scan now, **When** the request is accepted, **Then** the page confirms that a run has been queued and remains usable. It does not block until every repository has been checked.
3. **Given** a run is already queued or in progress for the operator, **When** another scan now is requested, **Then** the product keeps a single active run for that scope and tells the operator that a run is already underway.
4. **Given** GitHub temporarily limits request volume, **When** the inventory is still within its allowed window, **Then** the run slows or pauses and continues, and the stored run records that it was rate limited.
5. **Given** the run cannot finish every repository before it must stop, **When** the operator next opens the page, **Then** the run is shown as partial, with the repositories that were checked kept, and the rest marked not scanned.
6. **Given** the run checked a repository, **When** the results are stored, **Then** they include whether that repository has a non-empty description and a README, and for code of conduct, contributing guide, licence, security policy, issue templates, and pull request template they include whether the item is in the project repository and whether it is present only via an organisation default. The Open Source page does not display those checklist facts.
7. **Given** hosted sign-in is in use and the installation credential is configured, **When** the daily inventory runs with nobody signed in, **Then** the run finishes using that installation access and the next visit shows the new results.
8. **Given** a linked open-source repository is outside the GitHub App installation, **When** the run finishes, **Then** that repository is shown as not visible to the installation, with its funding status left blank.

---

### User Story 4 - Find funding hygiene on a shared Open Source page (Priority: P2)

Funding hygiene is the first section of an Open Source area in the main navigation. The area is named and structured so later open-source hygiene (for example community-file readiness) can appear beside it without a new top-level destination for each check.

**Why this priority**: The funding view is useful on its own. Putting it on a dedicated Open Source page avoids overloading the Repositories catalogue and gives later scans a home. The later sections themselves are not part of this feature.

**Independent Test**: From any signed-in page, open Open Source from the main navigation and land on funding hygiene. Confirm the page describes itself as open-source hygiene, not as a second repository catalogue, and that no community-standards section is presented as if it had data.

**Acceptance Scenarios**:

1. **Given** the operator is signed in, **When** they use the main navigation, **Then** they can open an Open Source page whose first working section is funding file hygiene.
2. **Given** the operator is on the Open Source page, **When** they look for catalogue management, **Then** they are not asked to reclassify repositories there. Open source still means the existing catalogue rule (the GitHub topic `open-source`).
3. **Given** later hygiene sections are not built yet, **When** the operator opens the page, **Then** the page does not show empty fake sections for those future checks.

---

### Edge Cases

- A repository classified as open source is archived, a fork, or private. It stays in the inventory. The row shows those traits so the operator can judge them. Archived repositories stay out of the "needs attention" count.
- The operator removes the `open-source` topic, or loses access to a repository, after a snapshot was stored. The next finished run drops that repository from the current view. The page does not keep showing it as a current gap.
- A repository is added to the open-source set after the last run. The page cross-checks the cached open-source catalogue against the displayed run's snapshots and shows the repository as not yet scanned until a run includes it. It is not shown as missing.
- The funding file exists but is empty, is not valid funding text, or is larger than 64 KiB (comparison limit). Empty or invalid text is present but differs with a short reason. Content that cannot be retrieved or exceeds the limit is not scanned with a short reason, never matches.
- The only funding file is at the repository root. That file is the one compared. Status is not "missing" merely because `.github/FUNDING.yml` is absent.
- An organisation `.github` repository supplies a default funding file and the project repository has none. The project row stays "missing from this repository". The row may note that an organisation default exists. This feature does not treat the organisation default as a match.
- The canonical file is saved as empty or as whitespace only. The product rejects it and keeps the previous standard if there was one.
- Two funding paths disagree. Comparison uses `.github/FUNDING.yml`. The row notes that a different root file also exists.
- The scheduled run starts while a manual run is in progress. They do not both walk the catalogue. One run completes; the other is skipped or coalesced, and the page shows a single last-finished result.
- The hosted installation credential is missing or revoked. The page says overnight inventory cannot run in hosted sign-in. It does not fall back to the signed-in user's session, and it does not show a blank success.
- A linked open-source repository sits outside the GitHub App installation. That repository is "not visible to the installation". Funding status and Community Standards facts are left unset. Repositories the installation can see are still inventoried.
- A community health file (code of conduct, contributing guide, licence, security policy, issue templates, or pull request template) exists only in the organisation `.github` repository. The project-repository fact is absent and the organisation-only fact is present. A copy in the project repository is present in the repository, and the organisation-only fact is false even when an organisation default also exists. Description and README have no organisation-default fact.
- The installation can see repositories that are not in the operator's open-source catalogue. Those repositories are not added to the run.
- A repository file matches the standard when the entire file uses only CRLF or only LF line endings (after normalisation per FR-010), or differs only by a single trailing newline. That file matches. A leading byte-order mark, or CRLF and LF mixed within the same file, is a difference.
- The operator uses hosted sign-in on one device while the inventory runs overnight on the server. The browser session is not required, and closing the browser does not cancel a run that has already started.
- Scan results from a previous credential are still stored after the operator switches between personal-token operation and hosted sign-in. The page shows which kind of access produced the latest run, and does not present another identity's snapshot as the current one.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The product MUST offer an Open Source page, linked from the main navigation, whose first section is funding-file hygiene for open-source repositories.
- **FR-002**: The page MUST read stored inventory results only. Opening or refreshing the page MUST NOT check every open-source repository live. Rows for open-source catalogue entries not present in the displayed run snapshots MUST show as not yet scanned using the cached catalogue only.
- **FR-003**: Each current open-source repository in the displayed run MUST show one funding status: missing from that repository, present and matching the standard, or present but different from the standard. Repositories that the run could not check MUST show not scanned, not a funding status. When no canonical standard existed at run start, checked repositories with a file MUST show present but not compared (not missing, match, or differ) until a later run compares after a standard is saved.
- **FR-004**: The page MUST show the latest run's finished time, whether the run was the daily schedule or a manual request, whether it completed or finished partial, and whether the results are stale. Results older than 36 hours are stale.
- **FR-005**: The operator MUST be able to request a scan now. The request MUST be accepted without waiting for the catalogue walk to finish. A further request while a run is queued or in progress MUST NOT start a second walk of the same scope.
- **FR-006**: The product MUST run the inventory once a day, by default, even when nobody has the product open and no browser session exists. Closing the browser MUST NOT be required to start the daily run, and MUST NOT be required for the daily run to finish.
- **FR-007**: The inventory scope MUST be the linked repositories already classified as open source by the GitHub topic `open-source`. Public visibility alone MUST NOT add a repository. The topic `oss` alone MUST NOT add a repository. Repository groups MUST NOT define this scope.
- **FR-008**: The inventory MUST include archived, forked, and private repositories when they are in that open-source set. The page MUST show those traits. The count of repositories that need attention MUST exclude archived repositories.
- **FR-009**: For each checked repository the inventory MUST record which funding file it compared, using this precedence: `.github/FUNDING.yml` when present, otherwise a root `FUNDING.yml`, otherwise missing. A root file MUST NOT override `.github/FUNDING.yml`.
- **FR-010**: The operator MUST be able to save one canonical funding file in the product. The inventory MUST compare repository text with that saved standard. "Matches" MUST mean the same text after normalising line endings to Unix newlines when the file uses a single line-ending style throughout (all CRLF or all LF), and ignoring a difference of one trailing newline only. A leading byte-order mark or mixed line endings within one file MUST count as differs. YAML key order, comments, quoting, and platform entries MUST count as differences. Files larger than 64 KiB MUST NOT be compared as text; the repository is not scanned with a short reason. The product MUST reject an empty standard.
- **FR-011**: When a repository has no funding file of its own, status MUST be missing from that repository even if an organisation default might apply on GitHub. The inventory MAY note that an organisation default was seen. That note MUST NOT count as a match.
- **FR-012**: The page MUST summarise how many in-scope repositories match, differ, are missing, or were not scanned, and MUST let the operator tell archived repositories apart from the ones that need attention. The operator MUST be able to open the repository on GitHub from the row.
- **FR-013**: A finished inventory run MUST record: when it started and finished; whether it was scheduled or manual; the access kind that produced it (operator credential or GitHub App installation); the open-source scope it intended to cover; overall outcome (completed, partial, or failed); and, per repository checked, the funding status, the path compared, whether an extra root or organisation-default file was noted, the funding platforms named in the file when the file could be read, the Community Standards checklist in FR-014, archived/fork/private traits, and a short failure reason when that repository was not checked.
- **FR-014**: For every repository the run checks, the inventory MUST record whether the repository description is non-empty and whether the project repository has a README. For each file-based Community Standards item (code of conduct, contributing guide, licence, security policy, issue templates, and pull request template) the inventory MUST record two facts: present in the project repository, and present only via an organisation default. When the item is in the project repository, the organisation-only fact MUST be false even if an organisation default also exists. When a licence is identified, the inventory MUST record a single licence name: the project repository's licence when that repository has one, otherwise the organisation-default licence name. The inventory MUST NOT store the body of those community files. The Open Source page MUST NOT display this checklist. A later feature reads the stored facts.
- **FR-015**: Unattended collection MUST NOT depend on a signed-in browser session. Personal-token (local and self-hosted) operation MUST use the same long-lived operator credential the site already uses for GitHub. Hosted sign-in MUST use a GitHub App installation credential that the server can renew with nobody signed in. The hosted run MUST NOT read or refresh the interactive session. When that installation credential is missing or revoked, hosted inventory MUST NOT run, and the page MUST say that hosted overnight inventory is unavailable.
- **FR-016**: The page MUST say which access kind produced the displayed run. A linked open-source repository the installation cannot see MUST be shown as not visible to the installation, with funding status and checklist facts left unset. The run MUST still record results for repositories the installation can see. When the latest attempt failed, the page MUST still show the last completed or partial snapshot for rows and MUST surface a banner that the most recent run failed (it MUST NOT present failure as a clean bill of health). The page MUST NOT present another identity's snapshot as the current one.
- **FR-017**: Inventory results MUST be kept per operator identity. One operator MUST NOT see another identity's latest run as their own.
- **FR-018**: The Open Source page MUST NOT apply funding files back to GitHub, edit repository topics, manage repository groups, or present a Community Standards section. Applying files, group management, and the checklist view remain later or existing work.
- **FR-019**: When the canonical file changes, existing rows MUST stay labelled with the standard they were compared against until a newer run finishes. The page MUST say when the visible comparison is out of date relative to the saved standard.
- **FR-020**: User-facing copy for the page, empty states, stale states, and errors MUST use UK English.

### Key Entities *(include if feature involves data)*

- **Canonical funding standard**: The single funding-file text the operator saves in the product. Attributes include the text, when it was saved, and a marker that inventories can store so a later edit does not silently reinterpret old rows.
- **Inventory run**: One unattended or manual pass over the operator's open-source repositories. Attributes include schedule versus manual, start and finish, access kind, intended scope, outcome, and whether GitHub limited the run.
- **Repository inventory snapshot**: The facts for one repository in one run. Funding attributes include status, path compared, extra-file note, and platforms named in the file. Community Standards attributes include whether the description is non-empty and whether the project repository has a README. For code of conduct, contributing guide, licence, security policy, issue templates, and pull request template, attributes include presence in the project repository and presence only via an organisation default. The licence name is the project repository's licence when it has one, otherwise the organisation-default licence name. Shared attributes include archived/fork/private traits, whether the installation could see the repository, and a failure reason when unchecked.
- **Open-source repository**: A linked repository the existing catalogue already classifies with the `open-source` topic. This feature does not create a second classification.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: An operator with a finished inventory can tell, within one minute of opening Open Source, which open-source repositories are missing the standard funding file or differ from it, without opening GitHub for each repository.
- **SC-002**: Opening Open Source with up to 500 in-scope repositories shows the stored summary and rows within 5 seconds, without a live check of those repositories.
- **SC-003**: A daily inventory that starts while the operator is signed out still appears on the next visit, with a last-finished time no more than 36 hours old when GitHub was available and the unattended credential was valid.
- **SC-004**: Scan now is acknowledged within 5 seconds, including when the catalogue contains hundreds of open-source repositories, and the page stays usable while the run continues.
- **SC-005**: In a review set of repositories that are missing the file, exactly matching the standard, or carrying a one-line drift, every repository receives the matching status. Line-ending-only differences are not reported as drift.
- **SC-006**: After the Open Source page is available, the operator can reach funding hygiene in one navigation step from the main menu, and the Repositories page is unchanged as the place that classifies open source.
- **SC-007**: For every repository a finished run checked, a follow-up review can tell whether the description and README are present on that repository, and for each of the other six Community Standards items whether it is in the project repository and whether it is present only via an organisation default, plus the licence name when one was identified, without opening GitHub and without those facts appearing on the Open Source page.
- **SC-008**: On a hosted deployment with a valid installation credential, a daily run that starts while nobody is signed in still produces a visible funding snapshot on the next visit. Each linked open-source repository the installation cannot see is labelled not visible, and no such repository is counted as missing a funding file.

## Assumptions

- Issue #439 is the source feature. The first inventory also stores the Community Standards checklist from issue #438 so that later page can read this snapshot. For the six health files, stored facts distinguish "in this repository" from "only via an organisation default". Description and README stay repository-only. This feature does not show that checklist. Issue #440's `open-source` topic rule is already shipped and is the scope rule. Issue #381 (repository groups) may later filter this page. It does not define who gets scanned.
- "Linked" means repositories already in the operator's Solo Dev Board catalogue. The inventory does not invent a second list, and it does not add repositories merely because the GitHub App is installed on them.
- Personal-token operation is the local and trusted self-hosted mode that already has a long-lived operator credential. That credential is valid while the browser is closed, so the daily run can use it directly.
- Hosted sign-in keeps the user's GitHub authorisation in the interactive session and refreshes it while that session is alive. The unattended run does not use that session. It uses the GitHub App installation credential, renewed by the server. The installation can see only repositories the app is installed on, so the page separates "not visible" from funding gaps. One installation belongs to one operator. A signed-in visitor does not receive another installation's latest run.
- GitHub still has no catalogue-wide funding status. The inventory has to look at each open-source repository. The page never does that look itself. The page MAY read the open-source catalogue from the existing warmed SQL cache only; if the cache is cold on first load, it MAY perform one catalogue list refresh and MUST NOT call per-repository GitHub APIs for funding.
- Comparison is textual, not a semantic reading of the funding format. Platforms named in the file are recorded so the operator can see sponsors entries, but they do not decide match versus differ.
- A funding file that cannot be interpreted still counts as present when bytes were read; status is differs with a short reason. Status is not scanned when the content could not be retrieved or exceeded the size limit.
- The freshness window is 36 hours: long enough to cover a missed overnight slot and a delayed retry, short enough that a silent failure is visible the next day.
- Needs-attention means non-archived repositories that are missing or different. Archived gaps stay visible and stay out of that count. Forks and private repositories stay in the count when they are missing or different, because the operator chose the open-source topic.
- One active inventory per operator is enough. The product does not run a parallel manual and scheduled pass.
- Applying the standard file back to repositories is out of scope, as are GitHub Sponsors billing, marketplace monetisation, and editing community files.
- The Open Source page uses the same page chrome as other operational pages (purpose line, primary actions, clear empty and stale states). A wireframe of that page is agreed before the page is built. Later sections are not stubbed in as empty panels.
- The first pass stores the latest useful snapshot for the page. It does not need a long history browser. Previous runs may be discarded once a newer finished run supersedes them, except that a failed run MUST NOT erase the last successful or partial snapshot.
- Rate limiting is handled by continuing more slowly inside the same run. The operator is not expected to retry repository by repository.
- User-facing documentation for the new page will be updated when the behaviour ships. That writing is part of delivery, not a separate product decision.
