# Feature Specification: Operator repository snapshot

**Feature Branch**: `001-operator-snapshot-scanner`

**Created**: 2026-11-23

**Status**: Draft

**Input**: User description: "Specify an enabler that collects one current snapshot for the signed-in solo developer (the operator), covering every repository linked to their personal GitHub account, public and private. The operator starts a collection and reads its status on the existing Repositories page. Later features read that snapshot. This specification does not design Community Standards or FUNDING pages."

## Clarifications

### Session 2026-11-23

- Q: After the site restarts in the lean local posture, what should happen to a snapshot that had already succeeded and to a collection that was still running? → A: On this machine, the last successful snapshot and an interrupted run both survive a restart. The lean explanation only says there is no schedule and the snapshot is not promised on another machine.
- Q: After a collection succeeds, what should the Repositories page show about the repositories in the snapshot? → A: Collection status only: the state, the relevant times, the count, and any short reason. Each snapshot entry still records which repository it is, whether it is public or private, whether it is archived, and whether it is a fork, for later features to read.
- Q: While a collection is running and the operator stays on the Repositories page, should the recorded count change without them leaving or reloading? → A: Yes. The state, times, and recorded count update while the operator stays on the Repositories page. They do not need to leave or reload.
- Q: Can one repository in the snapshot be both archived and a fork? → A: Yes. Each entry records the two facts separately. Both can be true for the same repository.
- Q: What must a snapshot entry record so a later feature can tell which repository it is? → A: Both the GitHub repository id and the owner/name. The id is the stable identity. The owner/name is the name later features can show.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Collect the current catalogue (Priority: P1)

The operator is on the Repositories page, the catalogue screen they already use. They choose a collection control that is separate from reloading the live catalogue. Opening the page does not start a collection. When the collection succeeds, they have one current snapshot of every repository linked to their personal GitHub account that this product can already show them. The page shows that the collection succeeded, when it finished, and how many repositories are in the snapshot. It does not list those repositories. The live catalogue stays the list on the page. Reading the kept snapshot does not ask GitHub again for the whole catalogue.

**Why this priority**: Later features need one current picture of the operator's repositories. Without a successful collection, those features have nothing to read.

**Independent Test**: Sign in as an operator whose personal account has a known set of linked repositories, start one collection from the Repositories page, and compare the finished snapshot with that set.

**Acceptance Scenarios**:

1. **Given** the operator opens the Repositories page, **When** the page is shown, **Then** no collection starts because of that visit.
2. **Given** the operator is on the Repositories page, **When** they reload the live catalogue, **Then** the collection status and any kept snapshot stay as they were.
3. **Given** the operator has linked repositories the product can already show, **When** they start a collection and it succeeds, **Then** the snapshot contains one repository entry for each of those repositories, and the page shows success, the time that successful collection finished, and that count. The page does not list the entries.
4. **Given** a successful snapshot is kept, **When** a later feature reads that snapshot, **Then** the catalogue in the snapshot is available without contacting GitHub again for the whole list.
5. **Given** a successful snapshot already exists, **When** a later collection succeeds, **Then** the kept snapshot is that new collection and the page shows the new finish time. Older runs are not listed.
6. **Given** the operator wants to start a collection or read its status, **When** they look through the product, **Then** the Repositories page is the only place with that start control and that status.
7. **Given** a collection succeeds, **When** the operator looks at the same repositories on GitHub, **Then** the collection has left repository contents there unchanged.
8. **Given** a successful snapshot, **When** a later feature reads one repository entry, **Then** that entry records the GitHub repository id, the owner/name, whether it is public or private, whether it is archived, and whether it is a fork. The id is the stable identity, and the owner/name is the name later features can show. Archived and fork are separate facts, and both can be true. The Repositories page shows the count and does not list the entries. Community-standard gaps and FUNDING status are not part of this feature.

---

### User Story 2 - Include a private repository (Priority: P1)

A successful snapshot is the operator's full personal catalogue, including private repositories. It is not limited to open-source repositories. Later views may filter. Archived repositories and forks that the product can already show are included. Each entry records archived and fork as separate facts, so a repository that is both keeps both. The Repositories page shows the count, not the entries.

**Why this priority**: A snapshot that dropped private repositories would mislead every later feature that trusts it as the operator's catalogue.

**Independent Test**: Use an account with at least one private repository, one archived repository, one fork, and one repository that is both archived and a fork, where the Repositories page can already show them. After one successful collection, confirm the page shows that count, and confirm each repository appears once in the snapshot with its visibility and both facts.

**Acceptance Scenarios**:

1. **Given** the operator's personal account has a private repository the product can already show, **When** a collection succeeds, **Then** that repository is one entry in the snapshot and is recorded as private. The Repositories page does not list that entry.
2. **Given** the catalogue already includes an archived repository or a fork, **When** a collection succeeds, **Then** that repository is one entry and records the archived fact and the fork fact separately.
3. **Given** the catalogue already includes a repository that is both archived and a fork, **When** a collection succeeds, **Then** that one entry records both facts as true.
4. **Given** the operator's account has both public and private linked repositories, **When** a collection succeeds, **Then** both kinds are in the snapshot.

---

### User Story 3 - See progress while still working (Priority: P1)

While a collection is running, the Repositories page shows that it is still moving: when it started, and how many repositories it has recorded so far. That count goes up as the walk proceeds. If the operator stays on the page, the state, times, and recorded count update without them leaving or reloading. The operator can keep using the rest of the product. A second collection cannot be started for the same operator until this run has succeeded, failed, or been interrupted.

**Why this priority**: A catalogue walk can take long enough that the operator must see movement and must not be stuck on this page.

**Independent Test**: Start a collection for an account with more than one linked repository. Stay on the Repositories page and confirm the recorded count rises before the run finishes, without leaving or reloading. Then start again if needed, leave for another page, and complete an ordinary task before the run ends.

**Acceptance Scenarios**:

1. **Given** a collection is in progress, **When** the operator views the Repositories page, **Then** they see that it is in progress, when it started, and how many repositories have been recorded so far.
2. **Given** a collection covering more than one repository is still running, **When** the operator stays on the Repositories page, **Then** the recorded count becomes higher than on the earlier look without them leaving or reloading.
3. **Given** a collection is in progress, **When** the operator leaves the Repositories page and uses another part of the product, **Then** that other part remains usable without waiting for the collection to finish.
4. **Given** a collection is in progress, **When** the operator looks for a way to start another collection, **Then** a second run is not started.

---

### User Story 4 - See that no snapshot exists yet (Priority: P2)

If no snapshot exists, the Repositories page says so. The product does not pretend a collection has happened. A successful collection of an account with no linked repositories is different: that run succeeded, finished at a stated time, and holds zero entries.

**Why this priority**: Later features, and the operator, must be able to tell "never collected" from "collected and empty".

**Independent Test**: Open the Repositories page for an operator who has never had a successful collection, and separately complete a collection for an account with no linked repositories.

**Acceptance Scenarios**:

1. **Given** no successful snapshot exists for the operator, **When** they view the Repositories page, **Then** the page states that no snapshot is kept and does not show a finish time or a count of collected repositories.
2. **Given** the operator has no linked repositories, **When** a collection succeeds, **Then** the page shows success, the finish time, and a count of zero, which is distinct from never having run.

---

### User Story 5 - Keep the last success when a run fails (Priority: P2)

When a collection fails, the operator sees a short reason they can act on, when it failed, and that the previous successful snapshot is unchanged. The short reason is one of: a sign-in or token problem, a GitHub rate limit, or an unexpected error. The product does not keep a log of past runs. Longer diagnostic detail stays with the person who runs the site, outside this feature.

**Why this priority**: A failed refresh must not wipe the snapshot later features already rely on, and the operator needs a reason they can act on.

**Independent Test**: Keep a successful snapshot, cause one of the three failures on the next collection, and confirm the reason, the failure time, and the unchanged earlier snapshot.

**Acceptance Scenarios**:

1. **Given** a successful snapshot exists, **When** a later collection fails, **Then** the page shows when it failed and one short reason (a sign-in or token problem, a GitHub rate limit, or an unexpected error), and the previous snapshot's entries and finish time are unchanged.
2. **Given** no successful snapshot exists, **When** the first collection fails, **Then** there is still no snapshot, and the page shows the short reason and when it failed.
3. **Given** several collections have been started over time, **When** the operator looks for earlier runs, **Then** only the current run's status is shown, together with the latest successful snapshot if one exists. No history of past runs is kept.
4. **Given** a collection has recorded some repositories and then fails, **When** the operator reads the page, **Then** those in-progress recordings are not presented as a new successful snapshot.

---

### User Story 6 - Recover after the site stops mid-run (Priority: P2)

If the site stops while a collection is running, the next visit shows that run as interrupted, with a short reason that the site stopped before the collection finished. The run is not shown as still in progress. The operator can start again. Any previous successful snapshot is unchanged.

**Why this priority**: A stop must not leave the operator looking at a run that will never finish.

**Independent Test**: Start a collection, stop the site before it finishes, open the Repositories page after the site is available again, and confirm the run is interrupted and that a new collection can be started.

**Acceptance Scenarios**:

1. **Given** a collection is in progress, **When** the site stops before it finishes and the operator next visits the Repositories page after the site is available again, **Then** the run is shown as interrupted and is not shown as in progress.
2. **Given** that interrupted visit, **When** the operator reads the reason, **Then** it states that the site stopped before the collection finished.
3. **Given** a previous successful snapshot and an interrupted run, **When** the operator views the page, **Then** that previous snapshot is unchanged and they can start a new collection.

---

### User Story 7 - Keep each operator's snapshot to themselves (Priority: P1)

One operator cannot read another operator's snapshot. Each signed-in operator sees only their own collection status and their own snapshot.

**Why this priority**: The snapshot includes private repositories. Showing one operator's catalogue to another would break trust in the product.

**Independent Test**: Complete a collection as operator A. Sign in as operator B and confirm none of A's entries, finish time, or run status appear. A's snapshot is unchanged.

**Acceptance Scenarios**:

1. **Given** operator A has a successful snapshot, **When** operator B views the Repositories page, **Then** B does not see A's repository entries, finish time, or collection status.
2. **Given** operator B has never collected, **When** operator B views the page while operator A has a snapshot, **Then** B sees their own empty state.
3. **Given** operator A has a snapshot, **When** operator B starts a collection, **Then** A's snapshot stays as it was.

---

### User Story 8 - Collect on demand locally, and keep the snapshot when hosted (Priority: P2)

In the lean local posture the operator can collect on demand. The product explains that nothing collects on a schedule, and that the snapshot is kept on this machine rather than promised on another machine. A site restart on this machine keeps the last successful snapshot, and a run that was in progress is shown as interrupted. In hosted sign-in the same snapshot is still there after the operator signs out and signs back in, and after the site restarts.

**Why this priority**: The same feature must stay usable without extra infrastructure, and a hosted operator must be able to trust that a finished snapshot survives a new sign-in and a restart.

**Independent Test**: In the lean local posture, read the explanation and finish one collection with no extra infrastructure. Restart the site and confirm the same snapshot is still there. Separately, restart during a run and confirm that run is shown as interrupted. In hosted sign-in, finish a collection, sign out, sign back in, restart the site, sign in again, and confirm the same snapshot is still there.

**Acceptance Scenarios**:

1. **Given** the lean local posture, **When** the operator views collection on the Repositories page, **Then** the product explains that nothing collects on a schedule and that the snapshot is kept on this machine rather than promised on another machine.
2. **Given** the lean local posture, **When** the operator starts a collection, **Then** they can complete it on demand without being asked to add infrastructure.
3. **Given** the lean local posture and a successful snapshot, **When** the site restarts and the operator views the Repositories page, **Then** the same snapshot is still there, including its finish time and repository entries.
4. **Given** the lean local posture and a collection in progress, **When** the site restarts and the operator next views the Repositories page, **Then** the run is shown as interrupted, it is not shown as in progress, and any previous successful snapshot is unchanged.
5. **Given** hosted sign-in and a successful snapshot, **When** the operator signs out and signs back in, **Then** the same snapshot is still there, including its finish time and repository entries.
6. **Given** hosted sign-in and a successful snapshot, **When** the site restarts and the operator signs in again, **Then** the same snapshot is still there.

---

### Edge Cases

- An account with no linked repositories can still have a successful snapshot: success, a finish time, and a count of zero. That is different from never having run, and different from a failed first run.
- A failed or interrupted run does not publish the repositories recorded so far as a new successful snapshot. The in-progress count describes the current run only.
- Leaving the Repositories page, using the rest of the product, or signing out does not by itself mark a run as interrupted. Interrupted means the site stopped before the run finished. If the site keeps running, the operator sees the resulting status when they return: still in progress, succeeded, or failed.
- Only one run per operator is in progress at a time. The start control does not begin a second run while one is in progress.
- The live catalogue on GitHub can change after a snapshot is taken. The snapshot stays as last collected until a later collection succeeds. Reloading the live catalogue does not change the snapshot.
- Repositories the Repositories page cannot already show for that operator are not added to the snapshot. Organisation-owned repositories appear only when they are already part of that personal catalogue.
- The operator-facing failure reasons are only a sign-in or token problem, a GitHub rate limit, or an unexpected error. Anything longer stays with the person who runs the site, outside this feature.
- A new successful collection replaces the previous snapshot. The product does not offer a list of older snapshots or older runs.
- The Repositories page shows collection status: the state, the relevant times, the count, and any short reason. It does not list the repositories inside the snapshot. The live catalogue remains the list on that page.
- A repository that is both archived and a fork is one snapshot entry with both facts recorded as true.
- Each snapshot entry records the GitHub repository id and the owner/name. The id is the stable identity. One GitHub repository id appears once in a successful snapshot.
- While the operator stays on the Repositories page, that collection status updates without them leaving or reloading. Reloading the live catalogue still leaves the snapshot unchanged.
- In the lean local posture, a site restart on this machine keeps the last successful snapshot. A run that was in progress is shown as interrupted. The snapshot is not promised on another machine.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The operator MUST be able to start a collection from the Repositories page.
- **FR-002**: Opening the Repositories page MUST leave collection status unchanged and MUST NOT start a collection.
- **FR-003**: The collection control MUST be separate from reloading the live catalogue. Reloading the live catalogue MUST NOT start a collection and MUST NOT change the kept snapshot.
- **FR-004**: The Repositories page MUST be the only new surface in this feature. The product MUST NOT add a scanner page or a settings page for this collection.
- **FR-005**: The Repositories page MUST show whether a collection has never run, is in progress, succeeded, failed, or was interrupted. When a successful snapshot is kept, the page MUST show how many repository entries that snapshot holds. The page MUST NOT list those repository entries. The live catalogue on the page remains the catalogue the operator already has. While the operator remains on the page, the collection status shown there MUST update without them leaving or reloading.
- **FR-006**: When a collection has succeeded at least once, the Repositories page MUST show when that last successful collection finished. That finish time MUST remain visible when a later run has failed or been interrupted.
- **FR-007**: While a collection is in progress, the Repositories page MUST show when it started and how many repositories have been recorded so far. That count MUST increase as further repositories are recorded during the same run. If the operator stays on the page, the state, the start time, and the recorded count MUST update without them leaving or reloading.
- **FR-008**: The operator MUST be able to keep using the rest of the product while a collection is in progress, including leaving the Repositories page and completing another ordinary task.
- **FR-009**: The product MUST allow only one in-progress collection per operator. It MUST NOT start a second collection for that operator while one is in progress.
- **FR-010**: When a collection fails, the Repositories page MUST show when it failed and one short reason the operator can act on: a sign-in or token problem, a GitHub rate limit, or an unexpected error.
- **FR-011**: A failed collection MUST leave the previous successful snapshot unchanged. If no successful snapshot exists, the page MUST still show that no snapshot is kept.
- **FR-012**: If the site stops before a run finishes, the next visit MUST show that run as interrupted, with a short reason that the site stopped before the collection finished, and MUST NOT show the run as in progress. The operator MUST be able to start a new collection. Any previous successful snapshot MUST be unchanged.
- **FR-013**: The product MUST NOT keep a log or list of past runs. Only the current run's status, and the latest successful snapshot if one exists, are kept. Longer diagnostic detail stays with the person who runs the site, outside this feature.
- **FR-014**: A successful snapshot MUST list every repository linked to that operator's personal GitHub account that the product can already show them, public and private. The snapshot MUST NOT be limited to open-source repositories.
- **FR-015**: Each repository in a successful snapshot MUST be one repository entry. The entry MUST record the GitHub repository id and the owner/name. The id is the stable identity. The owner/name is the name later features can show. The entry MUST record its visibility. The entry MUST record whether it is archived and whether it is a fork as separate facts. Both facts MUST be recorded as true when the repository is both archived and a fork. A successful snapshot MUST contain one entry per GitHub repository id.
- **FR-016**: A successful collection MUST result in one repository entry per linked repository in scope, so a catalogue of N repositories yields N entries. Zero linked repositories MUST yield a successful snapshot with zero entries and a finish time, which is distinct from never having run.
- **FR-017**: Reading the kept snapshot MUST NOT contact GitHub again for the whole catalogue.
- **FR-018**: One operator MUST NOT be able to read another operator's snapshot, collection status, or finish time.
- **FR-019**: In the lean local posture the operator MUST be able to collect on demand. The product MUST explain that nothing collects on a schedule, and that the snapshot is kept on this machine rather than promised on another machine. A site restart on this machine MUST keep the last successful snapshot. A run that was in progress when the site restarted MUST be shown as interrupted and MUST NOT be shown as in progress. The operator MUST NOT be asked to add infrastructure before they can collect.
- **FR-020**: In hosted sign-in, the same snapshot MUST still be there after the operator signs out and signs back in, and after the site restarts.
- **FR-021**: If no snapshot exists, the Repositories page MUST say so. The product MUST NOT show a finish time or a count that implies a collection has succeeded. The page MUST NOT list repository entries.
- **FR-022**: The operator snapshot MUST remain the single current collection for that operator, with room for later features to attach further facts (community-standard gaps, FUNDING status) without a second collection. This feature MUST NOT define those facts.
- **FR-023**: GitHub MUST remain the place where repository contents change. This feature MUST NOT write or copy repository files to GitHub.
- **FR-024**: Repositories recorded during an in-progress, failed, or interrupted run MUST NOT be presented as a new successful snapshot.

### Key Entities

- **Operator snapshot**: The single current collection for one operator. It holds that operator's repository entries from the latest successful collection, and it can later carry extra facts for other features. It does not hold a history of older collections. It is readable only by that operator. On this machine it remains after a site restart, including in the lean local posture. It is not promised on another machine. In hosted sign-in it also remains after the operator signs out and signs back in.
- **Repository entry**: One linked repository inside an operator snapshot. It records the GitHub repository id and the owner/name. The id is the stable identity. The owner/name is the name later features can show. It records visibility (public or private), and records whether it is archived and whether it is a fork as separate facts. Both can be true. It does not include community-standard gaps, FUNDING status, description, topics, or other later facts.
- **Collection run**: The operator-visible state of collection for that operator: never run, in progress, succeeded, failed, or interrupted. It includes timing (when the current run started, when it failed, or when the last successful collection finished), how many repositories are recorded so far while a run is in progress, how many repository entries the kept snapshot holds when one exists, and a short reason when the run failed or was interrupted. The Repositories page shows that status and those counts. It does not list repository entries.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: After one successful collection, an operator whose personal catalogue contains N linked repositories sees a count of N on the Repositories page. The snapshot holds N repository entries, and the page does not list them. A private repository in that catalogue is one of those N entries.
- **SC-002**: A second operator can see none of the first operator's repository entries, finish time, or collection status (zero items from the other operator).
- **SC-003**: After a successful collection, the operator can read the finish time on the Repositories page without opening repositories or walking the catalogue.
- **SC-004**: While a collection of more than one repository is in progress, the Repositories page shows when the run started and how many repositories have been recorded so far. An operator who stays on the page sees a higher count before the run finishes, without leaving or reloading.
- **SC-005**: After a failed run, the operator sees exactly one of the short reasons (a sign-in or token problem, a GitHub rate limit, or an unexpected error) and when it failed, and the previous successful snapshot's entries and finish time are still available and unchanged.
- **SC-006**: After the site stops mid-run, the next visit does not show the run as in progress. It shows the run as interrupted, and the operator can start a new collection.
- **SC-007**: While a collection is in progress, the operator can leave the Repositories page and complete another ordinary task in the product before the collection finishes.
- **SC-008**: In the lean local posture, the operator can complete one collection without adding any infrastructure beyond the site they are already using, and the page states that nothing collects on a schedule and that the snapshot is kept on this machine rather than promised on another machine. After a site restart on this machine, that snapshot is still there. A run that was in progress at restart is shown as interrupted.
- **SC-009**: In hosted sign-in, after a successful collection the same snapshot, including its finish time and repository entries, is still there after the operator signs out and signs back in, and still there after the site restarts.
- **SC-010**: When no snapshot exists, the Repositories page states that no snapshot is kept and shows no finish time. A successful collection of zero repositories shows success, a finish time, and a count of zero.

## Scope

### In scope

- Starting a collection from the Repositories page.
- Seeing progress, success, a count, a short failure reason, or a short interruption reason on the Repositories page. The page does not list snapshot entries.
- Keeping the latest successful snapshot for the signed-in operator.
- Isolating one operator's snapshot from another operator.
- On-demand collection in the lean local posture, with an explanation the operator can read. On this machine, a site restart keeps the last successful snapshot and shows an in-progress run as interrupted. The snapshot is not promised on another machine.
- Hosted sign-in behaviour the operator can notice: the snapshot remains after a new sign-in and after the site restarts.

### Out of scope

- The Community Standards page (#438) and the FUNDING page (#439), including any community-standard gaps or FUNDING status facts.
- Writing or copying files to GitHub.
- Unattended daily jobs or any schedule.
- Scan history, a stored log of runs, and longer diagnostic detail for the operator.
- A scanner page or a settings page.
- A list of snapshot entries on the Repositories page, and replacing the live catalogue with the snapshot.
- Per-repository hygiene results, acknowledgements, ignore lists, and alert roll-ups.
- Any store the operator would have to install or choose. The operator is not asked where the snapshot is kept. What they can observe is the lean and hosted behaviour above.

## Assumptions

- The snapshot covers the operator's full personal catalogue, including private repositories, archived repositories, and forks, limited to repositories the product can already show that operator. Later views may filter. This is a locked choice. Archived and fork are separate facts on each entry, and a repository that is both keeps both. Each entry records the GitHub repository id and the owner/name. The id is the stable identity.
- The set of repositories in a successful snapshot matches the personal catalogue the Repositories page can already show. Organisation-owned repositories are included only when they are already in that catalogue.
- The last successful snapshot is kept when a later run fails or is interrupted. A later success replaces that snapshot. There is no history of older snapshots.
- A run stopped because the site stopped is shown as interrupted, not as still running and not as failed for a sign-in, rate-limit, or unexpected-error reason.
- Leaving the page or signing out does not, by itself, count as the site stopping.
- Nothing in this feature collects on a schedule. Lean local posture collects only when the operator asks. The snapshot is kept on this machine, including after a site restart, and is not promised on another machine. A run in progress when the site restarts is shown as interrupted.
- Collection status and the start action live on the Repositories page. That status is the state, the relevant times, the count, and any short reason. The page does not list snapshot entries. Later features read those entries. While the operator stays on the page, that status updates without them leaving or reloading.
- Operator-facing text uses UK English.
- GitHub stays the place where repository contents change. This feature only collects a snapshot.
- Only one collection runs at a time for an operator.
- A successful run with zero linked repositories is a real snapshot and is distinct from never having collected.
- The operator snapshot is the collection later features will read. Room is left for extra facts. Those facts, and the pages that would show them, are not specified here.
- The person who runs the site may hold richer diagnostic detail outside this feature. The operator is not given a stored log.
