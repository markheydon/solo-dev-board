import AxeBuilder from '@axe-core/playwright';
import type { Result } from 'axe-core';
import { expect, type Page } from '@playwright/test';
import { THEME_PREFERENCE_STORAGE_KEY } from './themePreference';

/** Routes audited for WCAG 2.1 AA in the CI accessibility suite. */
export const accessibilityRoutes = [
  { path: '/', name: 'Home' },
  { path: '/about', name: 'About' },
  { path: '/auth/connectivity-error?reason=token-rejected', name: 'PAT connectivity error' },
  { path: '/audit-dashboard', name: 'Audit Dashboard' },
  { path: '/repositories', name: 'Repositories' },
  { path: '/migrate', name: 'One-Click Migration' },
  { path: '/labels', name: 'Label Manager' },
  { path: '/board-rules', name: 'Board Rules' },
  { path: '/triage', name: 'Triage' },
  { path: '/actions-templates', name: 'Actions Templates' },
  { path: '/planning/daily-focus', name: 'Planning — Daily Focus' },
  { path: '/planning/backlog', name: 'Planning Backlog Review' },
  { path: '/planning/iteration', name: 'Planning Iteration' },
] as const;

const wcagTags = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'] as const;

/** Route-specific loading indicators that must settle before page-level axe scans. */
const routeLoadingTestIds: Readonly<Record<string, readonly string[]>> = {
  '/repositories': ['repositories-loading-state'],
  '/audit-dashboard': ['audit-loading-state'],
  '/labels': ['labels-loading-state'],
  '/board-rules': ['board-rules-repositories-loading-state'],
  '/triage': ['triage-loading-repositories'],
  '/actions-templates': ['actions-templates-repositories-loading-state', 'actions-templates-loading-state'],
};

function getBlockingViolations(violations: Result['violations']) {
  return violations.filter(
    (violation) => violation.impact === 'critical' || violation.impact === 'serious',
  );
}

function formatViolationSummary(blocking: Result['violations']): string {
  return blocking
    .map((violation) => {
      const nodes = violation.nodes
        .slice(0, 5)
        .map((node) => `    - ${node.target.join(' ')}: ${node.failureSummary}`)
        .join('\n');
      return `${violation.id} (${violation.impact}): ${violation.help}\n${nodes}`;
    })
    .join('\n\n');
}

function assertNoBlockingViolations(blocking: Result['violations'], context: string): void {
  const summary = formatViolationSummary(blocking);
  expect(blocking, `Critical/serious accessibility violations on ${context}:\n${summary}`).toEqual([]);
}

/**
 * Runs axe-core against the current page for WCAG 2.1 A and AA rules.
 * Asserts that no critical or serious violations are present.
 */
export async function expectNoCriticalOrSeriousViolations(page: Page, context: string): Promise<void> {
  // Transient snackbars can animate mid-scan and report false colour-contrast failures.
  // Page-level audits therefore exclude snackbar overlays; snackbar contrast is covered
  // separately by expectNoCriticalOrSeriousViolationsOnSelector.
  const results = await new AxeBuilder({ page })
    .withTags([...wcagTags])
    .exclude('.mud-snackbar')
    .analyze();

  assertNoBlockingViolations(getBlockingViolations(results.violations), context);
}

/**
 * Runs axe-core against a specific selector on the current page.
 * Use for isolated component scans such as MudBlazor snackbars.
 */
export async function expectNoCriticalOrSeriousViolationsOnSelector(
  page: Page,
  selector: string,
  context: string,
): Promise<void> {
  const results = await new AxeBuilder({ page })
    .include(selector)
    .withTags([...wcagTags])
    .analyze();

  assertNoBlockingViolations(getBlockingViolations(results.violations), context);
}

const resolvedThemeButtonLabels: Readonly<Record<'light' | 'dark', string>> = {
  light: 'Theme: light. Activate dark mode.',
  dark: 'Theme: dark. Activate automatic mode.',
};

/** Waits until the shell theme preference has been applied to the document. */
export async function waitForResolvedTheme(page: Page, theme: 'light' | 'dark'): Promise<void> {
  await expect(page.getByRole('button', { name: resolvedThemeButtonLabels[theme] })).toBeVisible({
    timeout: 15_000,
  });
  await expect(page.locator('html')).toHaveCSS('color-scheme', theme);
}

function hasOpaqueBackground(backgroundColor: string): boolean {
  const rgbaMatch = backgroundColor.match(
    /^rgba\(\s*[\d.]+\s*,\s*[\d.]+\s*,\s*[\d.]+\s*,\s*([\d.]+)\s*\)$/,
  );

  if (rgbaMatch) {
    return Number.parseFloat(rgbaMatch[1]) >= 1;
  }

  return /^rgb\(\s*[\d.]+\s*,\s*[\d.]+\s*,\s*[\d.]+\s*\)$/.test(backgroundColor);
}

/**
 * Waits for MudBlazor filled buttons to finish ripple/opacity transitions.
 * Transient rgba backgrounds can report false colour-contrast failures in axe scans.
 */
async function waitForFilledButtonBackgroundsToSettle(page: Page): Promise<void> {
  const filledButtons = page.locator('button.mud-button-filled');

  await expect.poll(async () => {
    const count = await filledButtons.count();
    if (count === 0) {
      return true;
    }

    return filledButtons.evaluateAll((buttons) =>
      buttons.every((button) => hasOpaqueBackground(getComputedStyle(button).backgroundColor)),
    );
  }, { timeout: 5_000 }).toBe(true);
}

/** Waits for the page to finish rendering before accessibility scans. */
export async function waitForAccessibilityScanReady(
  page: Page,
  path: string,
  theme: 'light' | 'dark',
): Promise<void> {
  if (path.startsWith('/auth/connectivity-error')) {
    await expect(page.getByRole('heading', { name: 'GitHub connection problem' })).toBeVisible();
    return;
  }

  await expect(page.getByRole('navigation')).toBeVisible();
  await waitForResolvedTheme(page, theme);

  const routePath = path.split('?')[0];
  const loadingTestIds = routeLoadingTestIds[routePath] ?? [];

  for (const testId of loadingTestIds) {
    await expect(page.getByTestId(testId)).toBeHidden({ timeout: 15_000 });
  }

  // Filled primary controls are disabled during repository loads; wait for a stable enabled state.
  if (routePath === '/repositories') {
    await expect(page.getByTestId('repositories-reload-from-github-button')).toBeEnabled({ timeout: 15_000 });
  }

  if (
    routePath === '/planning/daily-focus'
    || routePath === '/planning/backlog'
    || routePath === '/planning/iteration'
    || routePath === '/planning/repos'
  ) {
    await expect(page.getByTestId('planning-shell')).toBeVisible();
    await expect(page.locator('[aria-label="Loading Planning"]')).toBeHidden({ timeout: 15_000 });
  }

  await waitForFilledButtonBackgroundsToSettle(page);
}

/** Seeds the browser theme preference before the next navigation. */
export async function seedThemePreference(
  page: Page,
  preference: 'system' | 'light' | 'dark',
): Promise<void> {
  await page.addInitScript(([key, value]) => {
    localStorage.setItem(key, value);
  }, [THEME_PREFERENCE_STORAGE_KEY, preference] as const);
}
