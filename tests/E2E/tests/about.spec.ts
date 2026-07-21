import { test, expect } from '@playwright/test';

test('about page shows deployment metadata from the shell menu', async ({ page }) => {
  await page.goto('/');

  await page.getByRole('button', { name: 'More options' }).click();
  await page.getByRole('link', { name: 'About' }).click();

  await expect(page).toHaveURL(/\/about$/);
  await expect(page).toHaveTitle(/About/);
  await expect(page.getByTestId('about-application-name')).toContainText('SoloDevBoard');
  await expect(page.getByTestId('about-version')).not.toBeEmpty();
  await expect(page.getByTestId('about-dotnet-version')).toHaveText(/\d+\.\d+/);
  await expect(page.getByTestId('about-repository-link')).toHaveAttribute('href', /github\.com/);
});
