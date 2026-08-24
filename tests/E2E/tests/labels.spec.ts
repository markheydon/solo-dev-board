import { test, expect } from '@playwright/test';

test.describe('Label Manager shell', () => {
  test('page renders taxonomy controls before any repository data is available', async ({ page }) => {
    await page.goto('/labels');

    await expect(page).toHaveTitle(/Label Manager/);
    await expect(page.getByRole('heading', { name: 'Label Manager' })).toBeVisible();
    await expect(page.getByTestId('label-manager-tab-strip')).toBeVisible();
    await expect(page.getByTestId('new-label-button')).toBeDisabled();
    await expect(page.getByTestId('load-labels-button')).toBeDisabled();
    await expect(page.getByTestId('bulk-delete-labels-button')).toBeDisabled();
    await expect(page.getByText('No active repositories are available for label analysis.')).toBeVisible({
      timeout: 15_000,
    });
  });

  test('label manager tabs remain available across taxonomy modes', async ({ page }) => {
    await page.goto('/labels');

    await expect(page.getByRole('tab', { name: 'Labels' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Recommended taxonomy' })).toBeVisible();
    await expect(page.getByRole('tab', { name: 'Synchronise' })).toBeVisible();

    await page.getByRole('tab', { name: 'Recommended taxonomy' }).click();
    await expect(page.getByRole('heading', { name: 'Apply recommended taxonomy' })).toBeVisible();
    await expect(page.getByTestId('preview-taxonomy-button')).toHaveText('Preview');
    await expect(page.getByTestId('remove-labels-outside-taxonomy-checkbox')).toBeVisible();
    await expect(page.getByTestId('keep-area-labels-checkbox')).toHaveCount(0);

    const removeOutsideCheckbox = page.getByTestId('remove-labels-outside-taxonomy-checkbox');
    if (await removeOutsideCheckbox.isEnabled()) {
      await removeOutsideCheckbox.click();
      await expect(page.getByTestId('keep-area-labels-checkbox')).toBeVisible();
    }

    await page.getByRole('tab', { name: 'Synchronise' }).click();
    await expect(page.getByRole('heading', { name: 'Synchronise labels' })).toBeVisible();
    await expect(page.getByTestId('sync-keep-area-labels-checkbox')).toBeVisible();
  });

  test('labels tab exposes bulk delete control disabled until rows are selected', async ({ page }) => {
    await page.goto('/labels');

    await expect(page.getByRole('tab', { name: 'Labels' })).toBeVisible();
    await expect(page.getByTestId('bulk-delete-labels-button')).toBeDisabled();
    await expect(page.getByTestId('bulk-delete-labels-button')).toHaveText('Delete');
  });
});
