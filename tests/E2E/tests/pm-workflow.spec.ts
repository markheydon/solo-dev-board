import { test, expect } from '@playwright/test';
import { navigateViaDrawer } from '../fixtures/navigation';

test.describe('PM Workflow', () => {
  test('drawer navigation opens the Daily Focus tab shell', async ({ page }) => {
    await page.goto('/');

    await navigateViaDrawer(page, 'PM Workflow');

    await expect(page).toHaveURL(/\/pm-workflow\/daily-focus$/);
    await expect(page).toHaveTitle(/PM Workflow — Daily Focus/);
    await expect(page.getByTestId('pm-workflow-shell')).toBeVisible();
    await expect(page.getByTestId('pm-workflow-tab-strip')).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Daily Focus' })).toBeVisible();
    await expect(page.getByTestId('pm-workflow-daily-focus-page')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Daily Focus' })).toBeVisible();
  });

  test('daily focus shows occupancy and recommendations, empty copy, or a load error', async ({ page }) => {
    await page.goto('/pm-workflow/daily-focus');

    await expect(page.getByTestId('pm-workflow-shell')).toBeVisible();

    const chromeError = page.getByTestId('pm-workflow-chrome-error');
    const noBoardAlert = page.getByTestId('pm-workflow-daily-focus-no-board');
    const occupancyRegion = page.getByTestId('pm-workflow-daily-focus-board-state');
    const occupancyError = page.getByTestId('pm-workflow-daily-focus-error');

    await expect(chromeError.or(noBoardAlert).or(occupancyRegion).or(occupancyError).first()).toBeVisible({
      timeout: 15_000,
    });

    if (await chromeError.isVisible()) {
      await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible();
      return;
    }

    if (await occupancyError.isVisible()) {
      await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible();
      await expect(occupancyError).toContainText('Unable to load board occupancy');
      return;
    }

    if (await occupancyRegion.isVisible()) {
      await expect(page.getByTestId('pm-workflow-daily-focus-stalled')).toBeVisible();
      const stalledReviewsRegion = page.getByTestId('pm-workflow-daily-focus-stalled-reviews');
      const stalledReviewsError = page.getByTestId('pm-workflow-daily-focus-stalled-reviews-error');
      await expect(stalledReviewsRegion.or(stalledReviewsError).first()).toBeVisible();
    }

    if (await occupancyRegion.isVisible()) {
      const recommendationsRegion = page.getByTestId('pm-workflow-daily-focus-recommendations');
      const recommendationsError = page.getByTestId('pm-workflow-daily-focus-recommendations-error');
      const recommendationsWarning = page.getByTestId('pm-workflow-daily-focus-recommendations-warning');
      await expect(recommendationsRegion.or(recommendationsError).or(recommendationsWarning).first()).toBeVisible();
      if (await recommendationsRegion.isVisible()) {
        await expect(
          page.getByRole('heading', { name: 'Recommended today (all included repositories)' }),
        ).toBeVisible();
      }
    }
  });

  test('repos tab shows threshold and exclusion regions or a chrome error', async ({ page }) => {
    await page.goto('/pm-workflow/repos');

    await expect(page.getByTestId('pm-workflow-shell')).toBeVisible();

    const chromeError = page.getByTestId('pm-workflow-chrome-error');
    const thresholdsRegion = page.getByTestId('pm-workflow-thresholds-region');
    const exclusionsRegion = page.getByTestId('pm-workflow-exclusions-region');

    await expect(chromeError.or(thresholdsRegion).first()).toBeVisible({ timeout: 15_000 });

    if (await chromeError.isVisible()) {
      await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible();
      return;
    }

    await expect(thresholdsRegion).toBeVisible();
    await expect(exclusionsRegion).toBeVisible();
    await expect(page.getByTestId('pm-workflow-participation-summary')).toBeVisible();
    await expect(
      page.getByTestId('pm-workflow-included-table').or(page.getByTestId('pm-workflow-no-included-text')),
    ).toBeVisible();
    await expect(page.getByTestId('pm-workflow-capacity-field')).toBeVisible();
    await expect(page.getByTestId('pm-workflow-exclude-autocomplete')).toBeVisible();
    await expect(page.getByTestId('pm-workflow-no-exclusions-text')).toBeVisible();
    await expect(page.getByTestId('pm-workflow-repository-summary-region')).toBeVisible();
    await expect(
      page
        .getByTestId('pm-workflow-repository-summary-table')
        .or(page.getByTestId('pm-workflow-repository-summary-empty'))
        .or(page.getByTestId('pm-workflow-repository-summary-error'))
        .or(page.getByTestId('pm-workflow-repository-summary-partial-failure'))
        .or(page.getByTestId('pm-workflow-repository-summary-loading')),
    ).toBeVisible();
  });

  test('backlog tab shows filters and urgency panels, empty copy, or a load error', async ({ page }) => {
    await page.goto('/pm-workflow/backlog');

    await expect(page.getByTestId('pm-workflow-shell')).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Backlog' })).toBeVisible();

    const chromeError = page.getByTestId('pm-workflow-chrome-error');
    const noBoardAlert = page.getByTestId('pm-workflow-backlog-no-board');
    const filters = page.getByTestId('pm-workflow-backlog-filters');
    const loadError = page.getByTestId('pm-workflow-backlog-error');

    await expect(chromeError.or(noBoardAlert).or(filters).or(loadError).first()).toBeVisible({
      timeout: 15_000,
    });

    if (await chromeError.isVisible()) {
      await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible();
      return;
    }

    if (await loadError.isVisible()) {
      await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible();
      await expect(loadError).toContainText('Unable to load the backlog');
      return;
    }

    if (await filters.isVisible()) {
      await expect(page.getByTestId('pm-workflow-backlog-panels')).toBeVisible();
      await expect(page.getByTestId('pm-workflow-backlog-urgent')).toBeVisible();
      await expect(page.getByTestId('pm-workflow-backlog-ready')).toBeVisible();
      await expect(page.getByTestId('pm-workflow-backlog-blocked')).toBeVisible();
      await expect(page.getByTestId('pm-workflow-backlog-search')).toBeVisible();
    }
  });
});

test.describe('Daily Focus', () => {
  test('direct navigation opens the Daily Focus route shell', async ({ page }) => {
    await page.goto('/pm-workflow/daily-focus');

    await expect(page).toHaveURL(/\/pm-workflow\/daily-focus$/);
    await expect(page).toHaveTitle(/PM Workflow — Daily Focus/);
    await expect(page.getByTestId('pm-workflow-shell')).toBeVisible();
    await expect(page.getByTestId('pm-workflow-tab-strip')).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Daily Focus' })).toBeVisible();
    await expect(page.getByTestId('pm-workflow-daily-focus-page')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Daily Focus' })).toBeVisible();
  });

  test('shows no-board instructional copy, empty board, or occupancy error', async ({ page }) => {
    await page.goto('/pm-workflow/daily-focus');

    await expect(page.getByTestId('pm-workflow-shell')).toBeVisible();

    const chromeError = page.getByTestId('pm-workflow-chrome-error');
    const noBoardAlert = page.getByTestId('pm-workflow-daily-focus-no-board');
    const occupancyRegion = page.getByTestId('pm-workflow-daily-focus-board-state');
    const emptyBoardAlert = page.getByTestId('pm-workflow-daily-focus-empty');
    const emptyBoardPaper = page.getByTestId('pm-workflow-daily-focus-empty-board');
    const occupancyError = page.getByTestId('pm-workflow-daily-focus-error');

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
      await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible();
      return;
    }

    if (await occupancyError.isVisible()) {
      await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible();
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
    await page.goto('/pm-workflow/repos');

    await expect(page).toHaveTitle(/PM Workflow — Repos/);
    await expect(page.getByTestId('pm-workflow-shell')).toBeVisible();
    await expect(page.getByTestId('pm-workflow-repos-page')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Repo Management' })).toBeVisible();

    const chromeError = page.getByTestId('pm-workflow-chrome-error');
    const thresholdsRegion = page.getByTestId('pm-workflow-thresholds-region');
    const exclusionsRegion = page.getByTestId('pm-workflow-exclusions-region');
    const noBoardAlert = page.getByTestId('pm-workflow-no-board-alert');

    await expect(chromeError.or(thresholdsRegion).first()).toBeVisible({ timeout: 15_000 });

    if (await chromeError.isVisible()) {
      await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible();
      return;
    }

    await expect(thresholdsRegion).toBeVisible();
    await expect(exclusionsRegion).toBeVisible();
    await expect(page.getByTestId('pm-workflow-participation-summary')).toBeVisible();
    await expect(
      page.getByTestId('pm-workflow-included-table').or(page.getByTestId('pm-workflow-no-included-text')),
    ).toBeVisible();
    await expect(page.getByTestId('pm-workflow-capacity-field')).toBeVisible();
    await expect(page.getByTestId('pm-workflow-stall-days-field')).toBeVisible();
    await expect(page.getByTestId('pm-workflow-neglect-days-field')).toBeVisible();
    await expect(page.getByTestId('pm-workflow-exclude-autocomplete')).toBeVisible();
    await expect(page.getByTestId('pm-workflow-no-exclusions-text')).toBeVisible();
    await expect(page.getByTestId('pm-workflow-repository-summary-region')).toBeVisible();
    await expect(
      page
        .getByTestId('pm-workflow-repository-summary-table')
        .or(page.getByTestId('pm-workflow-repository-summary-empty'))
        .or(page.getByTestId('pm-workflow-repository-summary-error'))
        .or(page.getByTestId('pm-workflow-repository-summary-partial-failure'))
        .or(page.getByTestId('pm-workflow-repository-summary-loading')),
    ).toBeVisible();
    await expect(noBoardAlert.or(page.getByTestId('pm-workflow-board-select')).first()).toBeVisible();
  });
});

test.describe('Backlog Review', () => {
  test('direct navigation opens the Backlog Review route shell', async ({ page }) => {
    await page.goto('/pm-workflow/backlog');

    await expect(page).toHaveURL(/\/pm-workflow\/backlog$/);
    await expect(page).toHaveTitle(/PM Workflow — Backlog/);
    await expect(page.getByTestId('pm-workflow-shell')).toBeVisible();
    await expect(page.getByTestId('pm-workflow-tab-strip')).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Backlog' })).toBeVisible();
    await expect(page.getByTestId('pm-workflow-backlog-page')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Backlog Review' })).toBeVisible();
  });

  test('shows no-board instructional copy, grouping panels, empty catalogue, or load error', async ({ page }) => {
    await page.goto('/pm-workflow/backlog');

    await expect(page.getByTestId('pm-workflow-shell')).toBeVisible();

    const chromeError = page.getByTestId('pm-workflow-chrome-error');
    const noBoardAlert = page.getByTestId('pm-workflow-backlog-no-board');
    const filters = page.getByTestId('pm-workflow-backlog-filters');
    const panels = page.getByTestId('pm-workflow-backlog-panels');
    const catalogueEmpty = page.getByTestId('pm-workflow-backlog-empty');
    const filterEmpty = page.getByTestId('pm-workflow-backlog-filter-empty');
    const loadError = page.getByTestId('pm-workflow-backlog-error');
    const warning = page.getByTestId('pm-workflow-backlog-warning');

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
      await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible();
      return;
    }

    if (await loadError.isVisible()) {
      await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible();
      await expect(loadError).toContainText('Unable to load the backlog');
      return;
    }

    if (await warning.isVisible()) {
      await expect(page.getByRole('button', { name: 'Retry' })).toBeVisible();
      await expect(warning).toContainText('failed to load');
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
      await expect(page.getByTestId('pm-workflow-backlog-urgent')).toBeVisible();
      await expect(page.getByTestId('pm-workflow-backlog-ready')).toBeVisible();
      await expect(page.getByTestId('pm-workflow-backlog-awaiting-triage')).toBeVisible();
      await expect(page.getByTestId('pm-workflow-backlog-blocked')).toBeVisible();
      await expect(page.getByTestId('pm-workflow-backlog-epics')).toBeVisible();
      await expect(page.getByTestId('pm-workflow-backlog-neglected')).toBeVisible();
      await expect(page.getByTestId('pm-workflow-backlog-search')).toBeVisible();
      await expect(page.getByTestId('pm-workflow-backlog-type-filter')).toBeVisible();
      await expect(page.getByTestId('pm-workflow-backlog-repo-filter')).toBeVisible();
    }
  });
});
