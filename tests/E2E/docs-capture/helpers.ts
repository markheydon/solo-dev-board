import { expect, type Page } from '@playwright/test';
import path from 'node:path';
import { seedThemePreference } from '../fixtures/accessibility';

/** Repository-root-relative output directory for Hugo static images. */
export const docsImagesRoot = path.resolve(__dirname, '../../../user-docs/static/images');

/**
 * Seeds light theme preference and opens a feature page ready for screenshot capture.
 * @param page Playwright page.
 * @param route Absolute path within the app (for example `/repositories`).
 */
export async function openFeatureForCapture(page: Page, route: string): Promise<void> {
  await page.addInitScript(() => {
    // Prefer light colour scheme for consistent documentation screenshots.
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: (query: string) => ({
        matches: query.includes('prefers-color-scheme: dark') ? false : false,
        media: query,
        onchange: null,
        addListener: () => undefined,
        removeListener: () => undefined,
        addEventListener: () => undefined,
        removeEventListener: () => undefined,
        dispatchEvent: () => false,
      }),
    });
  });

  await page.goto('/');
  await seedThemePreference(page, 'light');
  await page.goto(route);
  await expect(page.locator('#main-content')).toBeVisible({ timeout: 30_000 });
  // Allow Blazor Server and MudBlazor to settle before capturing.
  await page.waitForTimeout(1_500);
}

/**
 * Captures a full-page screenshot into the Hugo static images tree.
 * @param page Playwright page.
 * @param featureSlug Feature folder name under `user-docs/static/images/`.
 * @param fileName Kebab-case PNG file name, including the `.png` extension.
 */
export async function captureDocsScreenshot(
  page: Page,
  featureSlug: string,
  fileName: string,
): Promise<void> {
  const targetPath = path.join(docsImagesRoot, featureSlug, fileName);
  await page.screenshot({
    path: targetPath,
    fullPage: true,
    animations: 'disabled',
  });
}
