import { test, expect } from '@playwright/test';
import {
  accessibilityRoutes,
  expectNoCriticalOrSeriousViolations,
  expectNoCriticalOrSeriousViolationsOnSelector,
  seedThemePreference,
  waitForAccessibilityScanReady,
} from '../fixtures/accessibility';
import { THEME_PREFERENCE_STORAGE_KEY } from '../fixtures/themePreference';

test.describe('WCAG 2.1 AA accessibility', () => {
  test.describe('light mode', () => {
    test.beforeEach(async ({ page }) => {
      await page.addInitScript((storageKey) => {
        localStorage.setItem(storageKey, 'light');
      }, THEME_PREFERENCE_STORAGE_KEY);
    });

    for (const route of accessibilityRoutes) {
      test(`${route.name} (${route.path}) has no critical or serious axe violations`, async ({ page }) => {
        const response = await page.goto(route.path);
        expect(response?.ok() || response?.status() === 401).toBeTruthy();

        await waitForAccessibilityScanReady(page, route.path);
        await expectNoCriticalOrSeriousViolations(page, `${route.name} (light)`);
      });
    }
  });

  test.describe('dark mode', () => {
    test.beforeEach(async ({ page }) => {
      await page.addInitScript((storageKey) => {
        localStorage.setItem(storageKey, 'dark');
      }, THEME_PREFERENCE_STORAGE_KEY);
    });

    for (const route of accessibilityRoutes) {
      test(`${route.name} (${route.path}) has no critical or serious axe violations`, async ({ page }) => {
        const response = await page.goto(route.path);
        expect(response?.ok() || response?.status() === 401).toBeTruthy();

        await waitForAccessibilityScanReady(page, route.path);

        await expectNoCriticalOrSeriousViolations(page, `${route.name} (dark)`);
      });
    }
  });

  test('home shell exposes labelled navigation controls', async ({ page }) => {
    await seedThemePreference(page, 'light');
    await page.goto('/');

    await expect(page.getByRole('link', { name: 'Skip to main content' })).toBeAttached();
    await expect(page.getByRole('button', { name: 'Toggle navigation drawer' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Theme: light. Activate dark mode.' })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByRole('button', { name: 'More options' })).toBeVisible();
    await expect(page.getByRole('navigation')).toBeVisible();
    await expect(page.locator('#main-content')).toBeVisible();
  });

  test('automatic mode follows the system colour scheme preference', async ({ page }) => {
    await page.emulateMedia({ colorScheme: 'dark' });
    await page.addInitScript((storageKey) => {
      localStorage.setItem(storageKey, 'system');
    }, THEME_PREFERENCE_STORAGE_KEY);
    await page.goto('/');

    await expect(page.getByRole('button', { name: 'Theme: automatic (follow system). Activate light mode.' })).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('html')).toHaveCSS('color-scheme', 'dark');
  });

  test('info snackbar meets WCAG 2.1 AA contrast requirements', async ({ page }) => {
    await page.goto('/repositories');
    await expect(page.getByRole('heading', { name: 'Repositories', exact: true })).toBeVisible();
    await expect(page.getByTestId('repositories-loading-state')).toBeHidden({ timeout: 15_000 });

    await page.getByRole('button', { name: 'Add' }).click();
    await expect(page.locator('.mud-snackbar').first()).toBeVisible();

    await expectNoCriticalOrSeriousViolationsOnSelector(
      page,
      '.mud-snackbar',
      'Repositories placeholder snackbar',
    );
  });
});
