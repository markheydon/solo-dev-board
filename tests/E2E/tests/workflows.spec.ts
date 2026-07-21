import { test, expect } from '@playwright/test';

test.describe('Workflow template browser', () => {
  test('built-in templates load and can be filtered and selected', async ({ page }) => {
    await page.goto('/workflows');

    await expect(page.getByTestId('workflow-template-browser-region')).toBeVisible();
    await expect(page.getByTestId('workflow-templates-loading-state')).toBeHidden({ timeout: 15_000 });
    await expect(page.getByTestId('workflow-template-grid')).toBeVisible();
    await expect(page.getByTestId('workflow-template-card-1')).toBeVisible();

    await page.getByRole('searchbox', { name: 'Search templates by name, category, or tags' }).fill('.NET CI');
    await expect(page.getByRole('heading', { name: '.NET CI' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Dependabot Auto-Merge' })).toBeHidden();

    await page.getByTestId('workflow-template-select-1').click();
    await expect(page.getByTestId('workflow-template-detail-region')).not.toContainText(
      'Select a template to review YAML',
    );
    await expect(page.getByTestId('workflow-template-select-1')).toHaveText('Selected');
  });

  test('repository selector surfaces an error without a live GitHub connection', async ({ page }) => {
    await page.goto('/workflows');

    await expect(page.getByTestId('workflow-repositories-loading-state')).toBeHidden({ timeout: 15_000 });
    await expect(page.getByTestId('workflow-repositories-error-state')).toBeVisible();
    await expect(page.getByText('Unable to load repositories')).toBeVisible();
  });
});
