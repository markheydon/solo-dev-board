import { test, expect } from '@playwright/test';
import { featureRoutes, navigateViaDrawer } from '../fixtures/navigation';

test.describe('Feature navigation', () => {
  test('home dashboard lists all feature entry points', async ({ page }) => {
    await page.goto('/');

    await expect(page.getByRole('heading', { name: 'Home' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Open Audit Dashboard' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Open Repositories' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Open One-Click Migration' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Open Label Manager' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Open Board Rules Visualiser' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Open Triage UI' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Open Workflow Templates' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Open PM Workflow' })).toBeVisible();
  });

  test('home feature cards navigate to the matching feature page', async ({ page }) => {
    const featureCards = [
      { linkName: 'Open Audit Dashboard', path: '/audit-dashboard', title: /Audit Dashboard/ },
      { linkName: 'Open Repositories', path: '/repositories', title: /Repositories/ },
      { linkName: 'Open One-Click Migration', path: '/migrate', title: /One-Click Migration/ },
      { linkName: 'Open Label Manager', path: '/labels', title: /Label Manager/ },
      { linkName: 'Open Board Rules Visualiser', path: '/board-rules', title: /Board Rules Visualiser/ },
      { linkName: 'Open Triage UI', path: '/triage', title: /Triage UI/ },
      { linkName: 'Open Workflow Templates', path: '/workflows', title: /Workflow Templates/ },
      { linkName: 'Open PM Workflow', path: '/pm-workflow/daily-focus', title: /PM Workflow — Daily Focus/ },
    ] as const;

    for (const feature of featureCards) {
      await page.goto('/');
      await page.getByRole('link', { name: feature.linkName }).click();
      await expect(page).toHaveURL(new RegExp(`${feature.path}$`));
      await expect(page).toHaveTitle(feature.title);
    }
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
