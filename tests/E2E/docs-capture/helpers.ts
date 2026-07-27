import { expect, type Page } from '@playwright/test';
import path from 'node:path';
import { seedThemePreference } from '../fixtures/accessibility';

/** Canonical public repository used in documentation screenshots. */
export const DOCS_EXAMPLE_REPOSITORY = 'markheydon/solo-dev-board';

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
  // Allow Blazor Server and MudBlazor to settle before interacting.
  await page.waitForTimeout(1_500);
}

/**
 * Waits for a repository autocomplete control to be ready for selection.
 * @param page Playwright page.
 * @param autocompleteTestId data-testid of the MudAutocomplete input.
 */
export async function waitForRepositoryAutocompleteReady(
  page: Page,
  autocompleteTestId: string,
): Promise<void> {
  const autocomplete = page.getByTestId(autocompleteTestId);
  await expect(autocomplete).toBeVisible({ timeout: 30_000 });
  await expect(autocomplete).toBeEnabled({ timeout: 30_000 });
}

/**
 * Selects a repository via the shared MudAutocomplete repository selector.
 * @param page Playwright page.
 * @param autocompleteTestId data-testid of the MudAutocomplete input.
 * @param repositoryFullName Full repository name (for example `owner/repo`).
 */
export async function selectRepositoryInAutocomplete(
  page: Page,
  autocompleteTestId: string,
  repositoryFullName: string = DOCS_EXAMPLE_REPOSITORY,
): Promise<void> {
  await waitForRepositoryAutocompleteReady(page, autocompleteTestId);

  const autocomplete = page.getByTestId(autocompleteTestId);
  const searchTerm = repositoryFullName.includes('/')
    ? repositoryFullName.split('/').pop() ?? repositoryFullName
    : repositoryFullName;

  await autocomplete.click();
  await autocomplete.fill(searchTerm);
  await page.waitForTimeout(750);

  const option = page.getByRole('option', { name: repositoryFullName, exact: true });
  await expect(option).toBeVisible({ timeout: 15_000 });
  await option.click();
  await page.waitForTimeout(750);
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

/**
 * Prepares the Audit Dashboard with loaded KPI and health indicator data.
 * @param page Playwright page.
 */
export async function prepareAuditDashboardForCapture(page: Page): Promise<void> {
  await openFeatureForCapture(page, '/audit-dashboard');
  await selectRepositoryInAutocomplete(page, 'audit-repository-autocomplete');

  const loadButton = page.getByTestId('audit-load-selected-button');
  await expect(loadButton).toBeEnabled({ timeout: 15_000 });
  await loadButton.click();

  await expect(page.getByTestId('audit-kpi-summary-cards')).toBeVisible({ timeout: 60_000 });
  await expect(page.getByTestId('audit-summary-table')).toBeVisible({ timeout: 60_000 });
  await page.waitForTimeout(1_000);
}

/**
 * Prepares the Repositories page with a populated grid filtered to the example repository.
 * @param page Playwright page.
 */
export async function prepareRepositoriesForCapture(page: Page): Promise<void> {
  await openFeatureForCapture(page, '/repositories');
  await expect(page.getByTestId('repositories-grid')).toBeVisible({ timeout: 60_000 });

  const searchField = page.getByLabel('Search repositories');
  await searchField.fill('solo-dev-board');
  await page.waitForTimeout(750);
  await expect(page.getByRole('cell', { name: 'solo-dev-board' })).toBeVisible({ timeout: 15_000 });
}

/**
 * Prepares the Label Manager with labels loaded for the example repository.
 * @param page Playwright page.
 */
export async function prepareLabelManagerForCapture(page: Page): Promise<void> {
  await openFeatureForCapture(page, '/labels');
  await selectRepositoryInAutocomplete(page, 'repository-autocomplete');

  const loadButton = page.getByTestId('load-labels-button');
  await expect(loadButton).toBeEnabled({ timeout: 15_000 });
  await loadButton.click();

  await expect(page.getByTestId('labels-grid')).toBeVisible({ timeout: 60_000 });
  await page.waitForTimeout(1_000);
}

/**
 * Prepares One-Click Migration with the example repository selected in the setup form.
 * @param page Playwright page.
 */
export async function prepareMigrationForCapture(page: Page): Promise<void> {
  await openFeatureForCapture(page, '/migrate');
  await selectRepositoryInAutocomplete(page, 'migration-repository-autocomplete');
  await expect(page.getByTestId('selected-repositories')).toContainText(DOCS_EXAMPLE_REPOSITORY, {
    timeout: 15_000,
  });
  await page.waitForTimeout(750);
}

/**
 * Prepares the Board Rules Visualiser with board context loaded for the example repository.
 * @param page Playwright page.
 */
export async function prepareBoardRulesForCapture(page: Page): Promise<void> {
  await openFeatureForCapture(page, '/board-rules');
  await selectRepositoryInAutocomplete(page, 'board-rules-repository-autocomplete');

  const projectBoardSelect = page
    .getByTestId('board-rules-project-board-selector')
    .getByRole('combobox', { name: 'Project board' });

  await expect(projectBoardSelect).toBeEnabled({ timeout: 60_000 });
  await projectBoardSelect.click();

  const firstBoardOption = page.getByRole('option').first();
  await expect(firstBoardOption).toBeVisible({ timeout: 15_000 });
  const boardTitle = (await firstBoardOption.textContent())?.trim();
  await firstBoardOption.click();

  if (boardTitle) {
    await expect(projectBoardSelect).toContainText(boardTitle, { timeout: 15_000 });
  }

  await expect(page.getByTestId('board-rules-board-context-ready-state')).toBeVisible({
    timeout: 60_000,
  });
  await page.waitForTimeout(1_000);
}

/**
 * Prepares the Triage UI with an active session showing the first queue item.
 * @param page Playwright page.
 */
export async function prepareTriageForCapture(page: Page): Promise<void> {
  await openFeatureForCapture(page, '/triage');
  await selectRepositoryInAutocomplete(page, 'triage-repository-autocomplete');

  const startButton = page.getByTestId('triage-start-session-button');
  await expect(startButton).toBeEnabled({ timeout: 15_000 });
  await startButton.click();

  const sessionComplete = page.getByTestId('triage-session-complete-region');
  const itemDetail = page.getByTestId('triage-item-detail-region');
  await expect(sessionComplete.or(itemDetail)).toBeVisible({ timeout: 60_000 });
  await page.waitForTimeout(1_000);
}

/**
 * Prepares Workflow Templates with the example repository selected and a template open in the detail panel.
 * @param page Playwright page.
 */
export async function prepareWorkflowTemplatesForCapture(page: Page): Promise<void> {
  await openFeatureForCapture(page, '/workflows');
  await selectRepositoryInAutocomplete(page, 'workflow-repository-autocomplete');

  await expect(page.getByTestId('workflow-template-grid')).toBeVisible({ timeout: 60_000 });

  const firstTemplateCard = page.locator('[data-testid^="workflow-template-card-"]').first();
  await expect(firstTemplateCard).toBeVisible({ timeout: 15_000 });
  await firstTemplateCard.getByRole('button').first().click();

  await expect(page.getByTestId('workflow-template-yaml-preview')).toBeVisible({ timeout: 15_000 });
  await page.waitForTimeout(750);
}
