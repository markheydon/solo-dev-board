import AxeBuilder from '@axe-core/playwright';
import type { Result } from 'axe-core';
import { expect, type Page } from '@playwright/test';

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
  { path: '/workflows', name: 'Workflow Templates' },
] as const;

const wcagTags = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa'] as const;

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

/** Waits for the page to finish rendering before accessibility scans. */
export async function waitForAccessibilityScanReady(page: Page, path: string): Promise<void> {
  if (path.startsWith('/auth/connectivity-error')) {
    await expect(page.getByRole('heading', { name: 'GitHub connection problem' })).toBeVisible();
    return;
  }

  await expect(page.getByRole('navigation')).toBeVisible();
}

/** Enables dark mode via the shell theme toggle. */
export async function enableDarkMode(page: Page): Promise<void> {
  await page.getByRole('button', { name: 'Toggle dark mode' }).click();
  await expect(page.getByRole('button', { name: 'Toggle light mode' })).toBeVisible();
}
