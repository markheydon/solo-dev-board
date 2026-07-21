import { test, expect } from '@playwright/test';

test('health endpoint returns healthy', async ({ request }) => {
  const response = await request.get('/health');
  expect(response.ok()).toBeTruthy();
  await expect(response.text()).resolves.toContain('Healthy');
});

test('home page renders dashboard navigation', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveTitle(/Home — SoloDevBoard/);
  await expect(page.getByRole('navigation')).toBeVisible();
});
