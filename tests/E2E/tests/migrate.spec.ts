import { test, expect } from '@playwright/test';

test.describe('One-Click Migration shell', () => {
  test('migration setup shell renders with preview locked before migration starts', async ({ page }) => {
    await page.goto('/migrate');

    await expect(page).toHaveTitle(/One-Click Migration/);
    await expect(page.getByRole('heading', { name: 'One-Click Migration' })).toBeVisible();
    await expect(page.getByTestId('migration-workflow-controls-card')).toBeVisible();
    await expect(page.getByTestId('migration-workflow-controls-heading')).toHaveText('Migration setup');
    await expect(page.getByTestId('migration-preview-empty-state')).toBeVisible({ timeout: 15_000 });

    const loadError = page.getByTestId('migration-repositories-load-error');
    if (await loadError.isVisible()) {
      await expect(page.getByText(/GitHub API request failed while loading repositories/i)).toBeVisible();
      await expect(page.getByRole('button', { name: /try loading repositories again/i })).toBeVisible();
      return;
    }

    await expect(page.getByTestId('migration-scope-columns-switch')).toBeVisible();
    await expect(page.getByText('Project board columns')).toBeVisible();
    await expect(page.getByTestId('migration-preview-button')).toBeDisabled();
  });

  test('repository load failure surfaces inline with retry', async ({ page }) => {
    await page.goto('/migrate');

    await expect(page.getByTestId('migration-repositories-load-error')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText(/GitHub API request failed while loading repositories/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /try loading repositories again/i })).toBeVisible();
  });

  test('overwrite with labels scope shows keep area labels control when repositories load', async ({ page }) => {
    await page.goto('/migrate');

    const loadError = page.getByTestId('migration-repositories-load-error');
    if (await loadError.isVisible({ timeout: 15_000 })) {
      await expect(page.getByText(/GitHub API request failed while loading repositories/i)).toBeVisible();
      await expect(page.getByRole('button', { name: /try loading repositories again/i })).toBeVisible();
      return;
    }

    await page.getByTestId('migration-conflict-strategy-select').click();
    await page.getByRole('option', { name: 'Overwrite' }).click();

    const keepAreaCheckbox = page.getByTestId('migration-keep-area-labels-checkbox');
    await expect(keepAreaCheckbox).toBeVisible();
    await expect(keepAreaCheckbox).toBeChecked();
    await expect(page.getByText('Keep area/* labels')).toBeVisible();
  });
});
