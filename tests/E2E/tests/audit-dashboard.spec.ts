import { test, expect } from '@playwright/test';

test.describe('Audit Dashboard shell', () => {
  test('repository selector exposes reload from GitHub after repositories finish loading', async ({ page }) => {
    await page.goto('/audit-dashboard');

    await expect(page.getByTestId('audit-command-surface')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByTestId('audit-reload-from-github-button')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Reload from GitHub' })).toBeVisible();
  });

  test('page surfaces repository load failure without a live GitHub connection', async ({ page }) => {
    await page.goto('/audit-dashboard');

    await expect(page).toHaveTitle(/Audit Dashboard/);
    await expect(page.getByRole('heading', { name: 'Audit Dashboard' })).toBeVisible();
    await expect(page.getByTestId('audit-feedback-region')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText('Unable to load repositories')).toBeVisible();
  });
});
