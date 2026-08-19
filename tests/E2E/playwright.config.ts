import { defineConfig } from '@playwright/test';
import { getWebServerEnv } from './fixtures/webServerEnv';

const baseUrl = process.env.PLAYWRIGHT_BASE_URL ?? 'http://localhost:5080';
const reuseExistingServer = process.env.PLAYWRIGHT_REUSE_SERVER === '1';

export default defineConfig({
  testDir: './tests',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 2,
  reporter: process.env.CI
    ? [['github'], ['html', { open: 'never' }], ['list']]
    : [['list'], ['html', { open: 'never' }]],
  use: {
    baseURL: baseUrl,
    trace: 'on-first-retry',
    viewport: { width: 1400, height: 900 },
  },
  webServer: {
    command:
      'dotnet run --project ../../src/App/SoloDevBoard.App/SoloDevBoard.App.csproj --no-launch-profile --no-build',
    url: `${baseUrl}/health`,
    timeout: 120_000,
    reuseExistingServer,
    stdout: 'pipe',
    stderr: 'pipe',
    env: getWebServerEnv(),
  },
});
