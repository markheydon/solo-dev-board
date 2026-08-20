import { defineConfig } from '@playwright/test';

/**
 * Playwright config for manual documentation screenshot capture.
 * Excluded from CI because testDir points at ./docs-capture (sibling of ./tests).
 * Run against a local app with a real PAT and DocsCapture:Enabled=true.
 */
export default defineConfig({
  testDir: './docs-capture',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: 0,
  workers: 1,
  reporter: 'list',
  timeout: 120_000,
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:5080',
    ignoreHTTPSErrors: true,
    trace: 'off',
    viewport: { width: 1400, height: 900 },
    colorScheme: 'light',
  },
});
