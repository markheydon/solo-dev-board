import { test, expect, type Page } from '@playwright/test';
import { isHostedE2eMode } from '../fixtures/e2eAuthMode';

const welcomeSignInTimeoutMs = 30_000;

/** Waits for the hosted welcome landing to render after the Blazor circuit connects. */
async function expectWelcomeSignInVisible(page: Page): Promise<void> {
  await expect(page.getByTestId('welcome-sign-in')).toBeVisible({ timeout: welcomeSignInTimeoutMs });
  await expect(page.getByTestId('welcome-sign-in')).toContainText('Sign in with GitHub');
}

test.beforeEach(() => {
  test.skip(!isHostedE2eMode(), 'Hosted sign-in mode only');
});

test('unauthenticated home redirects to welcome sign-in', async ({ page }) => {
  await page.goto('/');

  await expect(page).toHaveURL(/\/welcome/);
  await expectWelcomeSignInVisible(page);
});

test('welcome page renders hosted sign-in landing', async ({ page }) => {
  await page.goto('/welcome');

  await expect(page).toHaveURL(/\/welcome/);
  await expectWelcomeSignInVisible(page);
  await expect(page.getByText(/sign in with your github account/i)).toBeVisible();
});

test('blazor negotiate succeeds before authentication', async ({ request }) => {
  const response = await request.post('/_blazor/negotiate?negotiateVersion=1', {
    headers: {
      'Content-Type': 'application/json',
    },
  });

  expect(response.status()).toBe(200);
  const body = await response.json();
  expect(body).toHaveProperty('connectionToken');
});

test('protected route does not render feature shell without sign-in', async ({ page }) => {
  await page.goto('/repositories');

  await expect(page).toHaveURL(/\/welcome/);
  await expectWelcomeSignInVisible(page);
  await expect(page.getByRole('heading', { name: /repositories/i })).toHaveCount(0);
});
