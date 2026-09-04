import { test, expect } from '@playwright/test';

test.describe('Workflow template browser', () => {
  test('built-in templates load and can be filtered and selected', async ({ page }) => {
    await page.goto('/actions-templates');

    await expect(page.getByTestId('actions-templates-custom-source-region')).toBeVisible();
    await expect(page.getByTestId('actions-templates-custom-source-field')).toBeVisible();
    await expect(page.getByTestId('actions-templates-custom-source-load')).toBeVisible();
    await expect(page.getByTestId('actions-templates-browser-region')).toBeVisible();
    await expect(page.getByTestId('actions-templates-loading-state')).toBeHidden({ timeout: 15_000 });
    await expect(page.getByTestId('actions-templates-grid')).toBeVisible();
    await expect(page.getByTestId('actions-templates-card-builtin:1')).toBeVisible();

    await page.getByRole('searchbox', { name: 'Search templates by name, category, or tags' }).fill('.NET CI');
    await expect(page.getByRole('heading', { name: '.NET CI' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Dependabot Auto-Merge' })).toBeHidden();

    await page.getByTestId('actions-templates-select-builtin:1').click();
    await expect(page.getByTestId('actions-templates-detail-region')).not.toContainText(
      'Select a template to review YAML',
    );
    await expect(page.getByTestId('actions-templates-select-builtin:1')).toHaveText('Selected');
  });

  test('built-in-only catalogue when custom source is empty', async ({ page }) => {
    await page.goto('/actions-templates');

    await expect(page.getByTestId('actions-templates-loading-state')).toBeHidden({ timeout: 15_000 });
    await expect(page.getByTestId('actions-templates-custom-source-field')).toHaveValue('');
    await expect(page.getByTestId('actions-templates-custom-source-load')).toBeDisabled();
    await expect(page.getByTestId('actions-templates-card-builtin:1')).toBeVisible();
    await expect(page.getByTestId('actions-templates-card-builtin:2')).toBeVisible();
    await expect(page.getByTestId('actions-templates-card-builtin:3')).toBeVisible();
    await expect(page.getByTestId('actions-templates-source-badge-builtin:1')).toHaveText('Built-in');
    await expect(page.locator('[data-testid^="actions-templates-source-badge-custom:"]')).toHaveCount(0);
    await expect(page.getByTestId('actions-templates-custom-source-error')).toBeHidden();
  });

  test('invalid custom source shows error while built-in templates remain visible', async ({ page }) => {
    await page.goto('/actions-templates');

    await expect(page.getByTestId('actions-templates-loading-state')).toBeHidden({ timeout: 15_000 });

    const customSourceField = page.getByRole('textbox', { name: 'Source repository' });
    await customSourceField.fill('invalid-source');
    await customSourceField.press('Tab');
    await expect(page.getByTestId('actions-templates-custom-source-load')).toBeEnabled();
    await page.getByTestId('actions-templates-custom-source-load').click();

    await expect(page.getByTestId('actions-templates-custom-source-error')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByTestId('actions-templates-custom-source-error')).toContainText('owner/repository format');
    await expect(page.getByTestId('actions-templates-card-builtin:1')).toBeVisible();
    await expect(page.getByTestId('actions-templates-card-builtin:2')).toBeVisible();
    await expect(page.getByTestId('actions-templates-card-builtin:3')).toBeVisible();
  });

  test('repository selector exposes reload from GitHub after repositories finish loading', async ({ page }) => {
    await page.goto('/actions-templates');

    await expect(page.getByTestId('actions-templates-repositories-loading-state')).toBeHidden({ timeout: 15_000 });
    await expect(page.getByTestId('actions-templates-reload-from-github-button')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Reload from GitHub' })).toBeVisible();
  });

  test('repository selector surfaces an error without a live GitHub connection', async ({ page }) => {
    await page.goto('/actions-templates');

    await expect(page.getByTestId('actions-templates-repositories-loading-state')).toBeHidden({ timeout: 15_000 });
    await expect(page.getByTestId('actions-templates-repositories-error-state')).toBeVisible();
    await expect(page.getByText('Unable to load repositories')).toBeVisible();
  });
});
