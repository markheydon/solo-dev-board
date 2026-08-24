import { test, expect } from '@playwright/test';
import { navigateViaDrawer } from '../fixtures/navigation';

test.describe('Planning', () => {
  test('drawer navigation opens the Daily Focus tab shell', async ({ page }) => {
    await page.goto('/');

    await navigateViaDrawer(page, 'Planning');

    await expect(page).toHaveURL(/\/planning\/daily-focus$/);
    await expect(page).toHaveTitle(/Planning — Daily Focus/);
    await expect(page.getByTestId('planning-shell')).toBeVisible();
    await expect(page.getByTestId('planning-tab-strip')).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Daily Focus' })).toBeVisible();
    await expect(page.getByTestId('planning-daily-focus-page')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Daily Focus' })).toBeVisible();
  });

  test('daily focus shows occupancy and recommendations, empty copy, or a load error', async ({ page }) => {
    await page.goto('/planning/daily-focus');

    await expect(page.getByTestId('planning-shell')).toBeVisible();

    const chromeError = page.getByTestId('planning-chrome-error');
    const noBoardAlert = page.getByTestId('planning-daily-focus-no-board');
    const occupancyRegion = page.getByTestId('planning-daily-focus-board-state');
    const occupancyError = page.getByTestId('planning-daily-focus-error');

    await expect(chromeError.or(noBoardAlert).or(occupancyRegion).or(occupancyError).first()).toBeVisible({
      timeout: 15_000,
    });

    if (await chromeError.isVisible()) {
      await expect(chromeError.getByRole('button', { name: 'Retry' })).toBeVisible();
      return;
    }

    if (await occupancyError.isVisible()) {
      await expect(occupancyError.getByRole('button', { name: 'Retry' })).toBeVisible();
      await expect(occupancyError).toContainText('Unable to load board occupancy');
      return;
    }

    if (await occupancyRegion.isVisible()) {
      await expect(page.getByTestId('planning-daily-focus-stalled')).toBeVisible();
      const stalledReviewsRegion = page.getByTestId('planning-daily-focus-stalled-reviews');
      const stalledReviewsError = page.getByTestId('planning-daily-focus-stalled-reviews-error');
      await expect(stalledReviewsRegion.or(stalledReviewsError).first()).toBeVisible();
    }

    if (await occupancyRegion.isVisible()) {
      const recommendationsRegion = page.getByTestId('planning-daily-focus-recommendations');
      const recommendationsError = page.getByTestId('planning-daily-focus-recommendations-error');
      const recommendationsWarning = page.getByTestId('planning-daily-focus-recommendations-warning');
      await expect(recommendationsRegion.or(recommendationsError).or(recommendationsWarning).first()).toBeVisible();
      if (await recommendationsRegion.isVisible()) {
        await expect(
          page.getByRole('heading', { name: 'Recommended today (all included repositories)' }),
        ).toBeVisible();
      }
    }
  });

  test('repos tab shows threshold and exclusion regions or a chrome error', async ({ page }) => {
    await page.goto('/planning/repos');

    await expect(page.getByTestId('planning-shell')).toBeVisible();

    const chromeError = page.getByTestId('planning-chrome-error');
    const thresholdsRegion = page.getByTestId('planning-thresholds-region');
    const exclusionsRegion = page.getByTestId('planning-exclusions-region');

    await expect(chromeError.or(thresholdsRegion).first()).toBeVisible({ timeout: 15_000 });

    if (await chromeError.isVisible()) {
      await expect(chromeError.getByRole('button', { name: 'Retry' })).toBeVisible();
      return;
    }

    await expect(thresholdsRegion).toBeVisible();
    await expect(exclusionsRegion).toBeVisible();
    await expect(page.getByTestId('planning-participation-summary')).toBeVisible();
    await expect(
      page.getByTestId('planning-included-table').or(page.getByTestId('planning-no-included-text')),
    ).toBeVisible();
    await expect(page.getByTestId('planning-capacity-field')).toBeVisible();
    await expect(page.getByTestId('planning-exclude-autocomplete')).toBeVisible();
    await expect(page.getByTestId('planning-no-exclusions-text')).toBeVisible();
    await expect(page.getByTestId('planning-repository-summary-region')).toBeVisible();
    await expect(
      page
        .getByTestId('planning-repository-summary-table')
        .or(page.getByTestId('planning-repository-summary-empty'))
        .or(page.getByTestId('planning-repository-summary-error'))
        .or(page.getByTestId('planning-repository-summary-partial-failure'))
        .or(page.getByTestId('planning-repository-summary-loading')),
    ).toBeVisible();
  });

  test('backlog tab shows filters and urgency panels, empty copy, or a load error', async ({ page }) => {
    await page.goto('/planning/backlog');

    await expect(page.getByTestId('planning-shell')).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Backlog' })).toBeVisible();

    const chromeError = page.getByTestId('planning-chrome-error');
    const noBoardAlert = page.getByTestId('planning-backlog-no-board');
    const filters = page.getByTestId('planning-backlog-filters');
    const loadError = page.getByTestId('planning-backlog-error');

    await expect(chromeError.or(noBoardAlert).or(filters).or(loadError).first()).toBeVisible({
      timeout: 15_000,
    });

    if (await chromeError.isVisible()) {
      await expect(chromeError.getByRole('button', { name: 'Retry' })).toBeVisible();
      return;
    }

    if (await loadError.isVisible()) {
      await expect(loadError.getByRole('button', { name: 'Retry' })).toBeVisible();
      await expect(loadError).toContainText('Unable to load the backlog');
      return;
    }

    if (await filters.isVisible()) {
      await expect(page.getByTestId('planning-backlog-panels')).toBeVisible();
      await expect(page.getByTestId('planning-backlog-urgent')).toBeVisible();
      await expect(page.getByTestId('planning-backlog-ready')).toBeVisible();
      await expect(page.getByTestId('planning-backlog-blocked')).toBeVisible();
      await expect(page.getByTestId('planning-backlog-search')).toBeVisible();
    }
  });
});

test.describe('Daily Focus', () => {
  test('direct navigation opens the Daily Focus route shell', async ({ page }) => {
    await page.goto('/planning/daily-focus');

    await expect(page).toHaveURL(/\/planning\/daily-focus$/);
    await expect(page).toHaveTitle(/Planning — Daily Focus/);
    await expect(page.getByTestId('planning-shell')).toBeVisible();
    await expect(page.getByTestId('planning-tab-strip')).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Daily Focus' })).toBeVisible();
    await expect(page.getByTestId('planning-daily-focus-page')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Daily Focus' })).toBeVisible();
  });

  test('shows no-board instructional copy, empty board, or occupancy error', async ({ page }) => {
    await page.goto('/planning/daily-focus');

    await expect(page.getByTestId('planning-shell')).toBeVisible();

    const chromeError = page.getByTestId('planning-chrome-error');
    const noBoardAlert = page.getByTestId('planning-daily-focus-no-board');
    const occupancyRegion = page.getByTestId('planning-daily-focus-board-state');
    const emptyBoardAlert = page.getByTestId('planning-daily-focus-empty');
    const emptyBoardPaper = page.getByTestId('planning-daily-focus-empty-board');
    const occupancyError = page.getByTestId('planning-daily-focus-error');

    await expect(
      chromeError
        .or(noBoardAlert)
        .or(occupancyRegion)
        .or(emptyBoardAlert)
        .or(emptyBoardPaper)
        .or(occupancyError)
        .first(),
    ).toBeVisible({ timeout: 15_000 });

    if (await chromeError.isVisible()) {
      await expect(chromeError.getByRole('button', { name: 'Retry' })).toBeVisible();
      return;
    }

    if (await occupancyError.isVisible()) {
      await expect(occupancyError.getByRole('button', { name: 'Retry' })).toBeVisible();
      await expect(occupancyError).toContainText('Unable to load board occupancy');
      return;
    }

    if (await noBoardAlert.isVisible()) {
      await expect(noBoardAlert).toContainText(
        'Select a planning board in the dropdown above to load Daily Focus occupancy and recommendations.',
      );
      return;
    }

    if (await occupancyRegion.isVisible()) {
      const hasEmptyBoardState = await emptyBoardAlert.or(emptyBoardPaper).first().isVisible();
      if (!hasEmptyBoardState) {
        return;
      }
    }

    await expect(emptyBoardAlert.or(emptyBoardPaper).first()).toBeVisible();
    await expect(
      page
        .getByText('This planning board has no items.')
        .or(page.getByText('This planning board has no Status options and no items yet.')),
    ).toBeVisible();
  });
});

test.describe('Repo Management', () => {
  test('repos tab shows planning thresholds, exclusions, and per-repository summary shell', async ({ page }) => {
    await page.goto('/planning/repos');

    await expect(page).toHaveTitle(/Planning — Repos/);
    await expect(page.getByTestId('planning-shell')).toBeVisible();
    await expect(page.getByTestId('planning-repos-page')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Repo Management' })).toBeVisible();

    const chromeError = page.getByTestId('planning-chrome-error');
    const thresholdsRegion = page.getByTestId('planning-thresholds-region');
    const exclusionsRegion = page.getByTestId('planning-exclusions-region');
    const noBoardAlert = page.getByTestId('planning-no-board-alert');

    await expect(chromeError.or(thresholdsRegion).first()).toBeVisible({ timeout: 15_000 });

    if (await chromeError.isVisible()) {
      await expect(chromeError.getByRole('button', { name: 'Retry' })).toBeVisible();
      return;
    }

    await expect(thresholdsRegion).toBeVisible();
    await expect(exclusionsRegion).toBeVisible();
    await expect(page.getByTestId('planning-participation-summary')).toBeVisible();
    await expect(
      page.getByTestId('planning-included-table').or(page.getByTestId('planning-no-included-text')),
    ).toBeVisible();
    await expect(page.getByTestId('planning-capacity-field')).toBeVisible();
    await expect(page.getByTestId('planning-stall-days-field')).toBeVisible();
    await expect(page.getByTestId('planning-neglect-days-field')).toBeVisible();
    await expect(page.getByTestId('planning-exclude-autocomplete')).toBeVisible();
    await expect(page.getByTestId('planning-no-exclusions-text')).toBeVisible();
    await expect(page.getByTestId('planning-repository-summary-region')).toBeVisible();
    await expect(
      page
        .getByTestId('planning-repository-summary-table')
        .or(page.getByTestId('planning-repository-summary-empty'))
        .or(page.getByTestId('planning-repository-summary-error'))
        .or(page.getByTestId('planning-repository-summary-partial-failure'))
        .or(page.getByTestId('planning-repository-summary-loading')),
    ).toBeVisible();

    if (await noBoardAlert.isVisible()) {
      await expect(noBoardAlert).toContainText('Select a planning board');
    } else if (await page.getByText(/Board:/).isVisible()) {
      await expect(page.getByTestId('planning-shared-chrome')).toContainText('Board:');
    } else {
      await expect(page.getByRole('combobox', { name: 'Planning board' })).toBeAttached();
    }
  });
});

test.describe('Iteration Planning', () => {
  test('direct navigation opens the Planning route shell', async ({ page }) => {
    await page.goto('/planning/iteration');

    await expect(page).toHaveURL(/\/planning\/iteration$/);
    await expect(page).toHaveTitle(/Planning — Iteration/);
    await expect(page.getByTestId('planning-shell')).toBeVisible();
    await expect(page.getByTestId('planning-tab-strip')).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Iteration' })).toBeVisible();
    await expect(page.getByTestId('planning-planning-page')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Iteration Planning' })).toBeVisible();
  });

  test('shows no-board instructional copy, Up Next and candidates, empty copy, or load error', async ({ page }) => {
    await page.goto('/planning/iteration');

    await expect(page.getByTestId('planning-shell')).toBeVisible();

    const chromeError = page.getByTestId('planning-chrome-error');
    const noBoardAlert = page.getByTestId('planning-planning-no-board');
    const upNextRegion = page.getByTestId('planning-planning-up-next');
    const candidatesRegion = page.getByTestId('planning-planning-candidates');
    const upNextEmpty = page.getByTestId('planning-planning-up-next-empty');
    const candidatesEmpty = page.getByTestId('planning-planning-candidates-empty');
    const loadError = page.getByTestId('planning-planning-error');
    const partialFailure = page.getByTestId('planning-planning-partial-failure');

    await expect(
      chromeError
        .or(noBoardAlert)
        .or(upNextRegion)
        .or(candidatesRegion)
        .or(upNextEmpty)
        .or(candidatesEmpty)
        .or(loadError)
        .or(partialFailure)
        .first(),
    ).toBeVisible({ timeout: 15_000 });

    if (await chromeError.isVisible()) {
      await expect(chromeError.getByRole('button', { name: 'Retry' })).toBeVisible();
      return;
    }

    if (await loadError.isVisible()) {
      await expect(loadError.getByRole('button', { name: 'Retry' })).toBeVisible();
      return;
    }

    if (await noBoardAlert.isVisible()) {
      await expect(noBoardAlert).toContainText(
        'Select a planning board in the dropdown above to load Up Next and candidate work items.',
      );
      return;
    }

    if (await partialFailure.isVisible()) {
      await expect(partialFailure).toContainText('failed');
    }

    if (await upNextRegion.isVisible()) {
      await expect(page.getByRole('heading', { name: 'This batch (Up Next)' })).toBeVisible();
      await expect(upNextEmpty.or(page.getByTestId('planning-planning-up-next-row')).first()).toBeVisible();
    }

    if (await candidatesRegion.isVisible()) {
      await expect(page.getByRole('heading', { name: 'Candidate picker' })).toBeVisible();
      await expect(page.getByTestId('planning-planning-search')).toBeVisible();
      await expect(page.getByTestId('planning-planning-show-issues')).toBeVisible();
      await expect(page.getByTestId('planning-planning-show-prs')).toBeVisible();
      await expect(
        candidatesEmpty.or(page.getByTestId('planning-planning-add-button')).first(),
      ).toBeVisible();
    }
  });
});

test.describe('Backlog Review', () => {
  test('direct navigation opens the Backlog Review route shell', async ({ page }) => {
    await page.goto('/planning/backlog');

    await expect(page).toHaveURL(/\/planning\/backlog$/);
    await expect(page).toHaveTitle(/Planning — Backlog/);
    await expect(page.getByTestId('planning-shell')).toBeVisible();
    await expect(page.getByTestId('planning-tab-strip')).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Backlog' })).toBeVisible();
    await expect(page.getByTestId('planning-backlog-page')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Backlog Review' })).toBeVisible();
  });

  test('shows no-board instructional copy, grouping panels, empty catalogue, or load error', async ({ page }) => {
    await page.goto('/planning/backlog');

    await expect(page.getByTestId('planning-shell')).toBeVisible();

    const chromeError = page.getByTestId('planning-chrome-error');
    const noBoardAlert = page.getByTestId('planning-backlog-no-board');
    const filters = page.getByTestId('planning-backlog-filters');
    const panels = page.getByTestId('planning-backlog-panels');
    const catalogueEmpty = page.getByTestId('planning-backlog-empty');
    const filterEmpty = page.getByTestId('planning-backlog-filter-empty');
    const loadError = page.getByTestId('planning-backlog-error');
    const warning = page.getByTestId('planning-backlog-warning');

    await expect(
      chromeError
        .or(noBoardAlert)
        .or(filters)
        .or(catalogueEmpty)
        .or(filterEmpty)
        .or(loadError)
        .or(warning)
        .first(),
    ).toBeVisible({ timeout: 15_000 });

    if (await chromeError.isVisible()) {
      await expect(chromeError.getByRole('button', { name: 'Retry' })).toBeVisible();
      return;
    }

    if (await loadError.isVisible()) {
      await expect(loadError.getByRole('button', { name: 'Retry' })).toBeVisible();
      await expect(loadError).toContainText('Unable to load the backlog');
      return;
    }

    if (await warning.isVisible()) {
      await expect(warning).toContainText('failed to load');
      await expect(page.getByTestId('planning-backlog-retry')).toBeVisible();
    }

    if (await noBoardAlert.isVisible()) {
      await expect(noBoardAlert).toContainText(
        'Select a planning board in the dropdown above to load Backlog Review groups.',
      );
      return;
    }

    if (await catalogueEmpty.isVisible()) {
      await expect(catalogueEmpty).toContainText('No open issues or pull requests in included repositories.');
      return;
    }

    if (await filterEmpty.isVisible()) {
      await expect(filterEmpty).toContainText('No items match the current filters.');
      return;
    }

    if (await filters.isVisible()) {
      await expect(panels).toBeVisible();
      await expect(page.getByTestId('planning-backlog-urgent')).toBeVisible();
      await expect(page.getByTestId('planning-backlog-ready')).toBeVisible();
      await expect(page.getByTestId('planning-backlog-awaiting-triage')).toBeVisible();
      await expect(page.getByTestId('planning-backlog-blocked')).toBeVisible();
      await expect(page.getByTestId('planning-backlog-epics')).toBeVisible();
      await expect(page.getByTestId('planning-backlog-neglected')).toBeVisible();
      await expect(page.getByTestId('planning-backlog-search')).toBeVisible();
      await expect(page.getByTestId('planning-backlog-type-filter')).toBeVisible();
      await expect(page.getByTestId('planning-backlog-repo-filter')).toBeVisible();
    }
  });
});
