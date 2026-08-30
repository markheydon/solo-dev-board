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
    const columnsSwitch = page.getByTestId('migration-scope-columns-switch');
    await expect(loadError.or(columnsSwitch)).toBeVisible({ timeout: 30_000 });

    if (await loadError.isVisible()) {
      await expect(page.getByText(/GitHub API request failed while loading repositories/i)).toBeVisible();
      await expect(page.getByRole('button', { name: /try loading repositories again/i })).toBeVisible();
      return;
    }

    await expect(columnsSwitch).toBeVisible();
    await expect(page.getByText('Project board columns')).toBeVisible();
    await expect(page.getByTestId('migration-preview-button')).toBeDisabled();
  });

  test('repository load failure surfaces inline with retry', async ({ page }) => {
    await page.goto('/migrate');

    await expect(page.getByTestId('migration-repositories-load-error')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText(/GitHub API request failed while loading repositories/i)).toBeVisible();
    await expect(page.getByRole('button', { name: /try loading repositories again/i })).toBeVisible();
  });

  test('labels scope shows ignore area labels control by default when repositories load', async ({ page }) => {
    await page.goto('/migrate');

    await expect(page.getByTestId('migration-workflow-controls-card')).toBeVisible({ timeout: 15_000 });

    const loadError = page.getByTestId('migration-repositories-load-error');
    const ignoreAreaCheckbox = page.getByTestId('migration-ignore-area-labels-checkbox');
    await expect(loadError.or(ignoreAreaCheckbox)).toBeVisible({ timeout: 30_000 });

    if (await loadError.isVisible()) {
      return;
    }

    await expect(ignoreAreaCheckbox).toBeVisible();
    await expect(ignoreAreaCheckbox).toBeChecked();
    await expect(page.getByText('Ignore area/* labels')).toBeVisible();
  });

  test('overwrite with labels scope shows keep area labels control when repositories load', async ({ page }) => {
    await page.goto('/migrate');

    await expect(page.getByTestId('migration-workflow-controls-card')).toBeVisible({ timeout: 15_000 });

    const loadError = page.getByTestId('migration-repositories-load-error');
    const conflictSelect = page.getByTestId('migration-conflict-strategy-select');
    await expect(loadError.or(conflictSelect)).toBeVisible({ timeout: 30_000 });

    if (await loadError.isVisible()) {
      return;
    }

    await conflictSelect.click();
    await page.getByRole('option', { name: 'Overwrite' }).click();

    const keepAreaCheckbox = page.getByTestId('migration-keep-area-labels-checkbox');
    await expect(keepAreaCheckbox).toBeVisible();
    await expect(keepAreaCheckbox).toBeChecked();
    await expect(page.getByText('Keep area/* labels')).toBeVisible();
  });
});
