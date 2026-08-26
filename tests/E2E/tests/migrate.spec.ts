import { test, expect } from '@playwright/test';

test.describe('One-Click Migration shell', () => {
  test('migration setup controls render and preview stays locked without repository data', async ({ page }) => {
    await page.goto('/migrate');

    await expect(page).toHaveTitle(/One-Click Migration/);
    await expect(page.getByRole('heading', { name: 'One-Click Migration' })).toBeVisible();
    await expect(page.getByTestId('migration-workflow-controls-card')).toBeVisible();
    await expect(page.getByTestId('migration-workflow-controls-heading')).toHaveText('Migration setup');
    await expect(page.getByTestId('migration-scope-columns-switch')).toBeVisible();
    await expect(page.getByText('Project board columns')).toBeVisible();
    await expect(page.getByTestId('migration-preview-button')).toBeDisabled();
    await expect(page.getByTestId('migration-preview-empty-state')).toBeVisible();
  });

  test('repository load failure surfaces inline with retry', async ({ page }) => {
    await page.goto('/migrate');

    await expect(page.getByTestId('migration-repositories-load-error')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText(/GitHub API request failed while loading repositories/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /try loading repositories again/i })).toBeVisible();
  });
});
