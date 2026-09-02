import { test, expect } from '@playwright/test';

test.describe('Workflow template browser', () => {
  test('built-in templates load and can be filtered and selected', async ({ page }) => {
    await page.goto('/actions-templates');

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

  test('repository selector surfaces an error without a live GitHub connection', async ({ page }) => {
    await page.goto('/actions-templates');

    await expect(page.getByTestId('actions-templates-repositories-loading-state')).toBeHidden({ timeout: 15_000 });
    await expect(page.getByTestId('actions-templates-repositories-error-state')).toBeVisible();
    await expect(page.getByText('Unable to load repositories')).toBeVisible();
  });
});
