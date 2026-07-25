import { test, expect } from '@playwright/test';

test.describe('Board Rules Visualiser shell', () => {
  test('selector region and compare mode toggle render before repository data is available', async ({ page }) => {
    await page.goto('/board-rules');

    await expect(page).toHaveTitle(/Board Rules Visualiser/);
    await expect(page.getByRole('heading', { name: 'Board Rules Visualiser' })).toBeVisible();
    await expect(page.getByTestId('board-rules-selector-region')).toBeVisible();
    await expect(page.getByTestId('board-rules-compare-mode-toggle')).toBeVisible();
    await expect(page.getByTestId('board-rules-visualisation-region')).toBeVisible();
  });

  test('repository load failure surfaces in the board rules feedback region', async ({ page }) => {
    await page.goto('/board-rules');

    await expect(page.getByTestId('board-rules-repositories-loading-state')).toBeHidden({ timeout: 15_000 });
    await expect(page.getByTestId('board-rules-error-alert')).toBeVisible();
    await expect(page.getByText(/GitHub API request failed/i)).toBeVisible();
    await expect(page.getByTestId('board-rules-reload-repositories-button')).toBeVisible();
  });
});
