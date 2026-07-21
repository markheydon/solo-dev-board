import { test, expect } from '@playwright/test';
import { featureRoutes, navigateViaDrawer } from '../fixtures/navigation';

test.describe('Feature navigation', () => {
  test('home dashboard lists all feature entry points', async ({ page }) => {
    await page.goto('/');

    await expect(page.getByRole('heading', { name: 'Home' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Open Label Manager' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Open Triage UI' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Open Workflow Templates' })).toBeVisible();
  });

  test('home feature cards navigate to the matching feature page', async ({ page }) => {
    await page.goto('/');

    await page.getByRole('link', { name: 'Open Label Manager' }).click();
    await expect(page).toHaveURL(/\/labels$/);
    await expect(page).toHaveTitle(/Label Manager/);

    await page.goto('/');
    await page.getByRole('link', { name: 'Open Workflow Templates' }).click();
    await expect(page).toHaveURL(/\/workflows$/);
    await expect(page).toHaveTitle(/Workflow Templates/);
  });

  test('drawer navigation reaches each primary feature route', async ({ page }) => {
    await page.goto('/');

    for (const feature of featureRoutes) {
      await navigateViaDrawer(page, feature.navLabel);
      await expect(page).toHaveURL(new RegExp(`${feature.path}$`));
      await expect(page).toHaveTitle(feature.title);
    }
  });
});
