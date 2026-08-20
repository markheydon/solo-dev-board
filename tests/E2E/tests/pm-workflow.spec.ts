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
});
