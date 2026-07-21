import { test, expect } from '@playwright/test';

test.describe('Triage shell', () => {
  test('page shows the not-started region without repository data', async ({ page }) => {
    await page.goto('/triage');

    await expect(page).toHaveTitle(/Triage UI/);
    await expect(page.getByRole('heading', { name: 'Triage UI' })).toBeVisible();
    await expect(page.getByTestId('triage-not-started-region')).toBeVisible();
    await expect(page.getByTestId('triage-no-repositories-alert')).toBeVisible({ timeout: 15_000 });
  });
});
