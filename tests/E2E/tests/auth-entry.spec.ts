import { test, expect } from '@playwright/test';

test('PAT connectivity error page returns token-rejected status and guidance', async ({ page }) => {
  const response = await page.goto('/auth/connectivity-error?reason=token-rejected');

  expect(response?.status()).toBe(401);
  await expect(page.getByRole('heading', { name: 'GitHub connection problem' })).toBeVisible();
  await expect(page.getByText(/personal access token/i)).toBeVisible();
  await expect(page.getByTestId('pat-connectivity-return-home')).toBeVisible();
});

test('welcome page redirects to home in PAT mode', async ({ page }) => {
  await page.goto('/welcome');

  await expect(page).toHaveURL(/\/$/);
  await expect(page).toHaveTitle(/Home — SoloDevBoard/);
});
