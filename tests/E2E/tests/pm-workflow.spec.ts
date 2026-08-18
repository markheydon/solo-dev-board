import { test, expect } from '@playwright/test';
import { navigateViaDrawer } from '../fixtures/navigation';

test.describe('PM Workflow Repo Management', () => {
  test('drawer navigation opens the Repos tab shell', async ({ page }) => {
    await page.goto('/');

    await navigateViaDrawer(page, 'PM Workflow');

    await expect(page).toHaveURL(/\/pm-workflow\/repos$/);
    await expect(page).toHaveTitle(/PM Workflow — Repos/);
    await expect(page.getByTestId('pm-workflow-shell')).toBeVisible();
    await expect(page.getByTestId('pm-workflow-tab-strip')).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Repos' })).toBeVisible();
    await expect(page.getByTestId('pm-workflow-repos-page')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Repo Management' })).toBeVisible();
  });

  test('repos tab shows threshold and exclusion regions or a chrome error', async ({ page }) => {
    await page.goto('/pm-workflow/repos');

    await expect(page.getByTestId('pm-workflow-shell')).toBeVisible();

    const chromeError = page.getByTestId('pm-workflow-chrome-error');
    const thresholdsRegion = page.getByTestId('pm-workflow-thresholds-region');
    const exclusionsRegion = page.getByTestId('pm-workflow-exclusions-region');

    await expect(chromeError.or(thresholdsRegion)).toBeVisible({ timeout: 15_000 });

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
  });
});
