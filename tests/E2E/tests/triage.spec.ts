import { test, expect } from '@playwright/test';

test.describe('Triage shell', () => {
  test('page shows repository load failure inline without a live GitHub connection', async ({ page }) => {
    await page.goto('/triage');

    await expect(page).toHaveTitle(/Triage UI/);
    await expect(page.getByRole('heading', { name: 'Triage UI' })).toBeVisible();
    await expect(page.getByTestId('triage-not-started-region')).toBeVisible();
    await expect(page.getByTestId('triage-repositories-load-error')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText(/GitHub API request failed while loading repositories/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /try loading repositories again/i })).toBeVisible();
  });

  test('action surface exposes disposition controls before a session starts only after session', async ({ page }) => {
    await page.goto('/triage');

    await expect(page.getByTestId('triage-not-started-region')).toBeVisible();
    await expect(page.getByTestId('triage-disposition-toggle')).toHaveCount(0);
  });
});
