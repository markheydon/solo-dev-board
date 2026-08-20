import { test, expect } from '@playwright/test';

test.describe('Repositories shell', () => {
  test('command strip and repository load failure are visible without a live GitHub connection', async ({ page }) => {
    await page.goto('/repositories');

    await expect(page).toHaveTitle(/Repositories/);
    await expect(page.getByRole('heading', { name: 'Repositories', exact: true })).toBeVisible();
    await expect(page.getByTestId('repositories-refresh-button')).toBeVisible();
    await expect(page.getByLabel('Search repositories')).toBeVisible();
    await expect(page.getByTestId('repositories-loading-state')).toBeHidden({ timeout: 15_000 });
    await expect(page.getByTestId('repositories-error-state')).toBeVisible();
    await expect(page.getByText('Unable to load repositories')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Try again' })).toBeVisible();
  });

  test('phone-width viewport does not introduce horizontal overflow on the repositories shell', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/repositories');

    await expect(page.getByTestId('repositories-loading-state')).toBeHidden({ timeout: 15_000 });
    await expect(page.getByTestId('repositories-error-state')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Actions' })).toBeVisible();

    const hasHorizontalOverflow = await page.evaluate(
      () => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1,
    );
    expect(hasHorizontalOverflow).toBeFalsy();
  });
});
