import { test, expect } from '@playwright/test';
import { THEME_PREFERENCE_STORAGE_KEY } from '../fixtures/themePreference';

test.describe('Appearance theme control', () => {
  test('theme button cycles automatic, light, and dark modes', async ({ page }) => {
    await page.goto('/');

    const themeButton = page.getByRole('button', { name: /^Theme:/ });
    await expect(themeButton).toBeVisible({ timeout: 15_000 });

    // Default is automatic when no preference is stored.
    await expect(themeButton).toHaveAccessibleName(/Theme: automatic \(follow system\)\. Activate light mode\./i);

    await themeButton.click();
    await expect(themeButton).toHaveAccessibleName(/Theme: light\. Activate dark mode\./i);

    await themeButton.click();
    await expect(themeButton).toHaveAccessibleName(/Theme: dark\. Activate automatic mode\./i);

    await themeButton.click();
    await expect(themeButton).toHaveAccessibleName(/Theme: automatic \(follow system\)\. Activate light mode\./i);
  });

  test('selected theme mode persists in browser storage', async ({ page }) => {
    await page.goto('/');

    const themeButton = page.getByRole('button', { name: /^Theme:/ });
    await expect(themeButton).toBeVisible({ timeout: 15_000 });
    await themeButton.click();

    await expect
      .poll(async () =>
        page.evaluate((storageKey) => localStorage.getItem(storageKey), THEME_PREFERENCE_STORAGE_KEY),
      )
      .toBe('light');

    await page.reload();
    await expect(themeButton).toHaveAccessibleName(/Theme: light\. Activate dark mode\./i);
  });
});
